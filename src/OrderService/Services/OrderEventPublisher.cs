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
    /// <summary>訂單完成事件的 Outbox EventType。</summary>
    public const string OrderCompletedType = "order.completed";

    /// <summary>訂單完成信（含下載連結）的 Outbox EventType。</summary>
    public const string OrderCompletedEmailType = "email.order_completed";

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
        outbox.Payload = JsonSerializer.Serialize(evt, OutboxJson.Options);
        db.OutboxMessages.Add(outbox);
    }

    /// <summary>寫入一筆訂單完成信 EmailRequestedEvent（模板 <c>order.completed</c>，含下載頁連結）。不負責 SaveChanges。</summary>
    public void AddOrderCompletedEmail(Order order, string storeName, string downloadUrl)
    {
        var outbox = new OutboxMessage { EventType = OrderCompletedEmailType };
        var evt = new EmailRequestedEvent(
            OutboxMessageId: outbox.Id,
            To: order.BuyerEmail,
            TemplateKey: "order.completed",
            Params: new Dictionary<string, string>
            {
                ["order_number"] = order.OrderNumber,
                ["store_name"]   = storeName,
                ["product_name"] = order.Items.Count == 1
                    ? order.Items[0].CatalogName
                    : $"{order.Items[0].CatalogName} 等 {order.Items.Count} 件商品",
                ["total_amount"] = (order.TotalAmount / 100m).ToString("0.##"),
                ["currency"]     = order.Currency.ToUpperInvariant(),
                ["download_url"] = downloadUrl,
            },
            Locale: "zh-TW");
        outbox.Payload = JsonSerializer.Serialize(evt, OutboxJson.Options);
        db.OutboxMessages.Add(outbox);
    }
}
