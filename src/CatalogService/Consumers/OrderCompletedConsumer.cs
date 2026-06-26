using CatalogService.Data;
using CatalogService.Data.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared.Events;

namespace CatalogService.Consumers;

/// <summary>
/// 消費 OrderCompletedEvent，累加對應商品的 SalesCount。
/// 以 ProcessedEvent（OutboxMessageId 唯一索引）去重，確保重複投遞冪等。
/// </summary>
public class OrderCompletedConsumer(
    CatalogDbContext db,
    ILogger<OrderCompletedConsumer> logger) : IConsumer<OrderCompletedEvent>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<OrderCompletedEvent> context)
    {
        var evt = context.Message;
        var ct  = context.CancellationToken;

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // 先 claim 去重鍵：撞唯一索引即代表此事件已處理過，冪等跳過（不重複累加）。
        db.ProcessedEvents.Add(new ProcessedEvent
        {
            OutboxMessageId = evt.OutboxMessageId,
            EventType       = "order.completed",
        });
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            logger.LogDebug("OrderCompletedEvent 重複，OutboxMessageId={Id}，略過", evt.OutboxMessageId);
            await tx.RollbackAsync(ct);
            return;
        }

        // 同一商品在訂單中可能出現多列，合併後一次原子累加（ExecuteUpdate 避免讀-改-寫競態）。
        var increments = evt.Items
            .GroupBy(i => i.CatalogId)
            .Select(g => new { CatalogId = g.Key, Count = g.LongCount() });

        foreach (var inc in increments)
        {
            await db.Catalogs
                .Where(c => c.Id == inc.CatalogId)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(c => c.SalesCount, c => c.SalesCount + inc.Count), ct);
        }

        await tx.CommitAsync(ct);
        logger.LogInformation(
            "Order {OrderId} 完成，累加 {Count} 件商品銷量。", evt.OrderId, evt.Items.Count);
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is Npgsql.PostgresException pg && pg.SqlState == "23505";
}
