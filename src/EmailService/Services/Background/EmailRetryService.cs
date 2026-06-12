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
/// （consumer claim 後、寫回結果前 crash 所致）。僅處理 LastAttemptAt 早於掃描間隔的紀錄，
/// 以避開 consumer 進行中的重試與前一輪掃描，降低重複寄送風險。
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

        // 只撈最後嘗試早於一個掃描間隔的紀錄，避開 consumer 進行中的重試與前一輪掃描
        var cutoff = DateTimeOffset.UtcNow.AddMinutes(-emailOptions.Value.RetryIntervalMinutes);

        var pending = await db.EmailRecords
            .Where(r => r.AttemptCount < maxAttempts
                     && (r.Status == EmailStatus.Failed || r.Status == EmailStatus.Pending)
                     && (r.LastAttemptAt == null || r.LastAttemptAt < cutoff))
            .ToListAsync(ct);

        if (pending.Count == 0) return;

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
    }
}
