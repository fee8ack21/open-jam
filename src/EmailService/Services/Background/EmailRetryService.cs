using EmailService.Data;
using EmailService.Data.Entities;
using EmailService.Options;
using EmailService.Services.Sending;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EmailService.Services.Background;

/// <summary>
/// 補償重試背景服務，定期掃描 Failed 且未達最大重試次數的紀錄並補發。
/// 作為 MassTransit 原生重試之後的最後保險。
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
        var db              = scope.ServiceProvider.GetRequiredService<EmailDbContext>();
        var emailSender     = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var maxAttempts     = emailOptions.Value.MaxRetryAttempts;

        var failed = await db.EmailRecords
            .Where(r => r.Status == EmailStatus.Failed && r.AttemptCount < maxAttempts)
            .ToListAsync(ct);

        if (failed.Count == 0) return;

        logger.LogInformation("EmailRetry: 重試 {Count} 筆失敗記錄", failed.Count);

        foreach (var record in failed)
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
