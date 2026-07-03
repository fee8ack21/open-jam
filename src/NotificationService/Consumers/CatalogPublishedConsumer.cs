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
/// 消費 CatalogPublishedEvent，為首次上架的商品建立即時通知任務（ScheduledAt = 上架當下），
/// 由 NotificationDispatcherService 對商店追蹤者 fan-out。重新上架（IsFirstPublish = false）不通知。
/// 以 ProcessedEvent（OutboxMessageId 唯一索引）去重，確保重複投遞冪等。
/// </summary>
public class CatalogPublishedConsumer(
    NotificationDbContext db,
    ILogger<CatalogPublishedConsumer> logger) : IConsumer<CatalogPublishedEvent>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<CatalogPublishedEvent> context)
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
                EventType       = NotificationTypes.CatalogPublished,
            });

            if (evt.IsFirstPublish)
            {
                db.NotificationRequests.Add(new NotificationRequest
                {
                    Type          = NotificationTypes.CatalogPublished,
                    StoreId       = evt.StoreId,
                    ScheduledAt   = evt.PublishedAt,
                    SourceEventId = evt.OutboxMessageId,
                    Payload = JsonSerializer.Serialize(new CatalogPublishedPayload
                    {
                        CatalogId   = evt.CatalogId,
                        CatalogName = evt.Name,
                        CatalogSlug = evt.Slug,
                        Price       = evt.Price,
                        Currency    = evt.Currency,
                    }, PayloadJson.Options),
                });
            }

            try
            {
                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                logger.LogInformation(
                    "CatalogPublishedEvent：商品 {CatalogId}（商店 {StoreId}）{Action}。",
                    evt.CatalogId, evt.StoreId, evt.IsFirstPublish ? "已建立通知任務" : "非首次上架，略過");
            }
            catch (DbUpdateException ex) when (PostgresErrors.IsUniqueViolation(ex))
            {
                // 撞 ProcessedEvent / SourceEventId 唯一索引 → 重複投遞，冪等跳過
                await tx.RollbackAsync(ct);
                logger.LogDebug("CatalogPublishedEvent 重複，OutboxMessageId={Id}，略過", evt.OutboxMessageId);
            }
        }, ct);
    }
}
