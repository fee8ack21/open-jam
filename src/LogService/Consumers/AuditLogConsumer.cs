using LogService.Data;
using LogService.Data.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared.Events;

namespace LogService.Consumers;

/// <summary>
/// 消費 AuditLogRequestedEvent，以 OutboxMessageId 去重後寫入 audit_log 資料表。
/// Append-only：只 INSERT，不執行任何 UPDATE / DELETE。
/// </summary>
public class AuditLogConsumer(
    LogDbContext db,
    ILogger<AuditLogConsumer> logger) : IConsumer<AuditLogRequestedEvent>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<AuditLogRequestedEvent> context)
    {
        var evt = context.Message;
        var ct  = context.CancellationToken;

        var log = new AuditLog
        {
            OutboxMessageId = evt.OutboxMessageId,
            Who             = evt.Who,
            Action          = evt.Action,
            Target          = evt.Target,
            TargetId        = evt.TargetId,
            Result          = evt.Result,
            Before          = evt.Before,
            After           = evt.After,
            Ip              = evt.Ip,
            UserAgent       = evt.UserAgent,
            Tenant          = evt.Tenant,
            OccurredAt      = evt.OccurredAt,
            CorrelationId   = evt.CorrelationId,
        };

        db.AuditLogs.Add(log);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // 重複訊息：已寫過，冪等跳過
            logger.LogDebug("AuditLog 重複訊息，OutboxMessageId={Id}，略過", evt.OutboxMessageId);
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is Npgsql.PostgresException pgEx && pgEx.SqlState == "23505";
}
