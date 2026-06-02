using EmailService.Data;
using EmailService.Data.Entities;
using EmailService.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared.Events;

namespace EmailService.Consumers;

public class EmailConsumer(
    EmailDbContext db,
    IEmailSender emailSender,
    ILogger<EmailConsumer> logger) : IConsumer<EmailRequestedEvent>
{
    public async Task Consume(ConsumeContext<EmailRequestedEvent> context)
    {
        var evt = context.Message;
        var ct  = context.CancellationToken;

        var existing = await db.EmailRecords
            .FirstOrDefaultAsync(r => r.OutboxMessageId == evt.OutboxMessageId, ct);

        if (existing?.Status == EmailStatus.Sent) return;

        var record = existing ?? new EmailRecord
        {
            Id              = Guid.NewGuid(),
            OutboxMessageId = evt.OutboxMessageId,
            To              = evt.To,
            Subject         = evt.Subject,
            BodyHtml        = evt.BodyHtml,
            Status          = EmailStatus.Pending,
            AttemptCount    = 0,
            CreatedAt       = DateTime.UtcNow
        };

        if (existing is null) db.EmailRecords.Add(record);

        record.AttemptCount++;
        record.LastAttemptAt = DateTime.UtcNow;

        try
        {
            await emailSender.SendAsync(record.To, record.Subject, record.BodyHtml, ct);
            record.Status = EmailStatus.Sent;
            record.SentAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            record.Status       = EmailStatus.Failed;
            record.ErrorMessage = ex.Message;
            logger.LogError(ex, "發信失敗，收件人: {To}", record.To);
        }

        await db.SaveChangesAsync(ct);
    }
}
