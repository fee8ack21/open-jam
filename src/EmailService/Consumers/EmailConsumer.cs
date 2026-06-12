using EmailService.Data;
using EmailService.Data.Entities;
using EmailService.Services.Sending;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared.Events;

namespace EmailService.Consumers;

/// <summary>
/// 消費 EmailRequestedEvent，渲染模板並寄信，以 OutboxMessageId 去重確保冪等。
/// <para>
/// 整個「claim → 寄送 → 落地狀態」包在單一資料庫交易內：claim 以 INSERT 取得 unique index 鎖，
/// 該鎖直到交易 commit（含寄送結果）才釋放，因此同一訊息的並發投遞會在 INSERT 處序列化等待，
/// 待前者完成後讀到 Sent 即跳過，避免重複寄送。撞 unique 違反時改以 FOR UPDATE 鎖既有列重判狀態，
/// 與 EmailRetryService 的 FOR UPDATE SKIP LOCKED 互相讓位。
/// </para>
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

        var translation = await db.EmailTemplateTranslations
            .Include(t => t.EmailTemplate)
            .FirstOrDefaultAsync(t => t.EmailTemplate.Key == evt.TemplateKey
                                   && t.Locale == evt.Locale, ct)
            ?? throw new InvalidOperationException(
                $"找不到模板 '{evt.TemplateKey}' / '{evt.Locale}'");

        var subject  = Render(translation.Subject,  evt.Params);
        var bodyHtml = Render(translation.BodyHtml, evt.Params);

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // 先寫 claim：嘗試插入 Pending 紀錄取得去重鎖；以 savepoint 包覆，撞 unique 後可回退續用同一交易
        var record = new EmailRecord
        {
            OutboxMessageId = evt.OutboxMessageId,
            To              = evt.To,
            Subject         = subject,
            BodyHtml        = bodyHtml,
            Status          = EmailStatus.Pending,
        };

        await tx.CreateSavepointAsync("claim", ct);
        try
        {
            db.EmailRecords.Add(record);
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // 重複訊息：回退 savepoint，以 FOR UPDATE 鎖既有列再判斷是否已寄出
            await tx.RollbackToSavepointAsync("claim", ct);
            db.ChangeTracker.Clear();

            var existing = (await db.EmailRecords
                .FromSql($"""
                    SELECT * FROM email_records
                    WHERE outbox_message_id = {evt.OutboxMessageId}
                    FOR UPDATE
                    """)
                .ToListAsync(ct)).FirstOrDefault();

            if (existing is null || existing.Status == EmailStatus.Sent)
            {
                await tx.CommitAsync(ct);
                return;
            }

            // 之前失敗或卡住的紀錄 → 持鎖續寄
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
        await tx.CommitAsync(ct);
    }

    private static string Render(string template, Dictionary<string, string> parameters) =>
        parameters.Aggregate(template, (s, kv) => s.Replace("{{" + kv.Key + "}}", kv.Value));

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is Npgsql.PostgresException pgEx && pgEx.SqlState == "23505";
}
