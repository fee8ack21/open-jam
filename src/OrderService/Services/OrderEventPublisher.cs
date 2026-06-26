using System.Text.Json;
using OrderService.Data;
using OrderService.Data.Entities;
using Shared.Events;

namespace OrderService.Services;

/// <summary>
/// 將訂單領域事件寫入 Outbox（業務 transaction 內），由 <see cref="Background.OutboxRelayService"/> 搬入 RabbitMQ。
/// Payload 以 snake_case 序列化，與 relay 的反序列化設定對稱，確保正確 round-trip。
/// </summary>
public class OrderEventPublisher(OrderDbContext db)
{
    /// <summary>Outbox payload 序列化設定，須與 OutboxRelayService 的反序列化一致。</summary>
    internal static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    /// <summary>訂單完成事件的 Outbox EventType。</summary>
    public const string OrderCompletedType = "order.completed";

    /// <summary>寫入一筆 OrderCompletedEvent（攜帶商品明細）。不負責 SaveChanges。</summary>
    public void AddOrderCompleted(Order order)
    {
        var outbox = new OutboxMessage { EventType = OrderCompletedType };
        var evt = new OrderCompletedEvent(
            OutboxMessageId: outbox.Id,
            OrderId: order.Id,
            StoreId: order.StoreId,
            CompletedAt: order.CompletedAt ?? DateTimeOffset.UtcNow,
            Items: order.Items
                .Select(i => new OrderCompletedItem(i.CatalogId, i.CatalogVersionId))
                .ToList());
        outbox.Payload = JsonSerializer.Serialize(evt, JsonOpts);
        db.OutboxMessages.Add(outbox);
    }
}
