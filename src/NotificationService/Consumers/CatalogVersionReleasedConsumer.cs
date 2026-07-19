using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using NotificationService.Data;
using NotificationService.Data.Entities;
using NotificationService.Models;
using Shared.Data;
using Shared.Events;

namespace NotificationService.Consumers;

/// <summary>
/// 消費 CatalogVersionReleasedEvent（創作者對已上架商品的版本按下「通知買家」），
/// 建立即時通知任務，由 NotificationDispatcherService 對該商品的既有買家（CatalogBuyerRef）fan-out。
/// 以 ProcessedEvent（OutboxMessageId 唯一索引）去重，確保重複投遞冪等。
/// </summary>
public class CatalogVersionReleasedConsumer(
    NotificationDbContext db,
    ILogger<CatalogVersionReleasedConsumer> logger) : IConsumer<CatalogVersionReleasedEvent>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<CatalogVersionReleasedEvent> context)
    {
        var evt = context.Message;
        var ct  = context.CancellationToken;

        await db.Database.ExecuteInTransactionAsync(async tx =>
        {
            // 重試 execution strategy 會整段重放，先清空前次嘗試殘留的追蹤實體
            db.ChangeTracker.Clear();

            db.ProcessedEvents.Add(new ProcessedEvent
            {
                OutboxMessageId = evt.OutboxMessageId,
                EventType       = NotificationTypes.CatalogVersionReleased,
            });

            db.NotificationRequests.Add(new NotificationRequest
            {
                Type          = NotificationTypes.CatalogVersionReleased,
                StoreId       = evt.StoreId,
                ScheduledAt   = evt.ReleasedAt,
                SourceEventId = evt.OutboxMessageId,
                Payload = JsonSerializer.Serialize(new CatalogVersionReleasedPayload
                {
                    CatalogId   = evt.CatalogId,
                    CatalogName = evt.CatalogName,
                    CatalogSlug = evt.CatalogSlug,
                    VersionId   = evt.VersionId,
                    Version     = evt.Version,
                    ReleaseNote = evt.ReleaseNote,
                }, PayloadJson.Options),
            });

            try
            {
                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                logger.LogInformation(
                    "CatalogVersionReleasedEvent：商品 {CatalogId} 版本 {Version} 已建立買家通知任務。",
                    evt.CatalogId, evt.Version);
            }
            catch (DbUpdateException ex) when (PostgresErrors.IsUniqueViolation(ex))
            {
                // 撞 ProcessedEvent / SourceEventId 唯一索引 → 重複投遞，冪等跳過
                await tx.RollbackAsync(ct);
                logger.LogDebug("CatalogVersionReleasedEvent 重複，OutboxMessageId={Id}，略過", evt.OutboxMessageId);
            }
        }, ct);
    }
}
