using EmailService.Data;
using EmailService.Data.Entities;
using EmailService.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EmailService.Services;

public class EmailRetryService(
    IServiceScopeFactory scopeFactory,
    IOptions<EmailOptions> emailOptions,
    ILogger<EmailRetryService> logger) : BackgroundService
{
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
        using var scope   = scopeFactory.CreateScope();
        var db            = scope.ServiceProvider.GetRequiredService<EmailDbContext>();
        var emailSender   = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var maxAttempts   = emailOptions.Value.MaxRetryAttempts;

        var failed = await db.EmailRecords
            .Where(r => r.Status == EmailStatus.Failed && r.AttemptCount < maxAttempts)
            .ToListAsync(ct);

        if (failed.Count == 0) return;

        logger.LogInformation("EmailRetry: 重試 {Count} 筆失敗記錄", failed.Count);

        foreach (var record in failed)
        {
            record.AttemptCount++;
            record.LastAttemptAt = DateTime.UtcNow;

            try
            {
                await emailSender.SendAsync(record.To, record.Subject, record.BodyHtml, ct);
                record.Status       = EmailStatus.Sent;
                record.SentAt       = DateTime.UtcNow;
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
