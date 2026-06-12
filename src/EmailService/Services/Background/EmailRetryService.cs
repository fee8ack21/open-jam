using EmailService.Data;
using EmailService.Data.Entities;
using EmailService.Options;
using EmailService.Services.Sending;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EmailService.Services.Background;

/// <summary>
/// 補償重試背景服務，作為 MassTransit 原生重試之後的最後保險。
/// 定期掃描未達最大重試次數的：(1) Failed 紀錄；(2) 卡在 Pending 的孤兒紀錄
/// （consumer claim 後、寫回結果前 crash 所致）。以 FOR UPDATE SKIP LOCKED 在交易內 claim 紀錄，
/// 跳過 consumer 正持鎖處理中的列；並以 LastAttemptAt 早於掃描間隔為門檻，避開剛被動過的紀錄，
/// 防止重複寄送。
/// </summary>
public class EmailRetryService(
    IServiceScopeFactory scopeFactory,
    IOptions<EmailOptions> emailOptions,
    ILogger<EmailRetryService> logger) : BackgroundService
{
    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await RetryFailedAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "EmailRetryService 發生錯誤");
            }

            await Task.Delay(
                TimeSpan.FromMinutes(emailOptions.Value.RetryIntervalMinutes), ct);
        }
    }

    private async Task RetryFailedAsync(CancellationToken ct)
    {
        using var scope     = scopeFactory.CreateScope();
        var db              = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var emailSender     = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var maxAttempts     = emailOptions.Value.MaxRetryAttempts;

        // 只撈最後嘗試早於一個掃描間隔的紀錄，避開剛被動過的紀錄
        var cutoff = DateTimeOffset.UtcNow.AddMinutes(-emailOptions.Value.RetryIntervalMinutes);

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // FOR UPDATE SKIP LOCKED：claim 本輪要處理的列並鎖住，跳過 consumer 正在處理（持鎖）的列；
        // 鎖持有至交易 commit，期間 consumer 對同列的 FOR UPDATE 會等待，避免重複寄送
        var pending = await db.EmailRecords
            .FromSql($"""
                SELECT * FROM email_records
                WHERE attempt_count < {maxAttempts}
                  AND status IN ('Failed', 'Pending')
                  AND (last_attempt_at IS NULL OR last_attempt_at < {cutoff})
                ORDER BY created_at
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(ct);

        if (pending.Count == 0)
        {
            await tx.CommitAsync(ct);
            return;
        }

        logger.LogInformation("EmailRetry: 重試 {Count} 筆待補發記錄", pending.Count);

        foreach (var record in pending)
        {
            record.AttemptCount++;
            record.LastAttemptAt = DateTimeOffset.UtcNow;

            try
            {
                await emailSender.SendAsync(record.To, record.Subject, record.BodyHtml, ct);
                record.Status       = EmailStatus.Sent;
                record.SentAt       = DateTimeOffset.UtcNow;
                record.ErrorMessage = null;
            }
            catch (Exception ex)
            {
                record.Status       = EmailStatus.Failed;
                record.ErrorMessage = ex.Message;
                logger.LogError(ex, "EmailRecord {Id} 重試失敗", record.Id);
            }
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}
