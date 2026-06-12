using EmailService.Data;
using EmailService.Data.Entities;
using EmailService.Services.Sending;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared.Events;

namespace EmailService.Consumers;

/// <summary>
/// 消費 EmailRequestedEvent，渲染模板並寄信，以 OutboxMessageId 去重確保冪等。
/// 採「先寫 claim 再寄」策略：先 INSERT 取得鎖，捕捉 unique constraint 例外判斷是否已處理。
/// </summary>
public class EmailConsumer(
    AppDbContext db,
    IEmailSender emailSender,
    ILogger<EmailConsumer> logger) : IConsumer<EmailRequestedEvent>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<EmailRequestedEvent> context)
    {
        var evt = context.Message;
        var ct  = context.CancellationToken;

        var translation = await db.EmailConfigTranslations
            .Include(t => t.EmailConfig)
            .FirstOrDefaultAsync(t => t.EmailConfig.TemplateKey == evt.TemplateKey
                                   && t.Locale == evt.Locale, ct)
            ?? throw new InvalidOperationException(
                $"找不到模板 '{evt.TemplateKey}' / '{evt.Locale}'");

        var subject  = Render(translation.Subject,  evt.Params);
        var bodyHtml = Render(translation.BodyHtml, evt.Params);

        // 先寫 claim：嘗試插入 Pending 紀錄取得去重鎖
        var record = new EmailRecord
        {
            OutboxMessageId = evt.OutboxMessageId,
            To              = evt.To,
            Subject         = subject,
            BodyHtml        = bodyHtml,
            Status          = EmailStatus.Pending,
        };

        db.EmailRecords.Add(record);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // 重複訊息：確認是否已成功寄出
            db.ChangeTracker.Clear();
            var existing = await db.EmailRecords
                .FirstOrDefaultAsync(r => r.OutboxMessageId == evt.OutboxMessageId, ct);

            if (existing is null || existing.Status == EmailStatus.Sent)
                return;

            // 之前失敗的紀錄 → 繼續重試
            record = existing;
        }

        record.AttemptCount++;
        record.LastAttemptAt = DateTimeOffset.UtcNow;

        try
        {
            await emailSender.SendAsync(record.To, record.Subject, record.BodyHtml, ct);
            record.Status = EmailStatus.Sent;
            record.SentAt = DateTimeOffset.UtcNow;
        }
        catch (Exception ex)
        {
            record.Status       = EmailStatus.Failed;
            record.ErrorMessage = ex.Message;
            logger.LogError(ex, "發信失敗，收件人: {To}", record.To);
        }

        await db.SaveChangesAsync(ct);
    }

    private static string Render(string template, Dictionary<string, string> parameters) =>
        parameters.Aggregate(template, (s, kv) => s.Replace("{{" + kv.Key + "}}", kv.Value));

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is Npgsql.PostgresException pgEx && pgEx.SqlState == "23505";
}
