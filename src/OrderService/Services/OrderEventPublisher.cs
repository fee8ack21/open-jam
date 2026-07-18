using System.Net;
using System.Text;
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

    /// <summary>台北時區（購買日期以買家在地時間呈現；平台目前僅服務台灣市場）。</summary>
    private static readonly TimeSpan TaipeiOffset = TimeSpan.FromHours(8);

    /// <summary>寫入一筆訂單完成信 EmailRequestedEvent（模板 <c>order.completed</c>，含下載頁連結與逐項購買明細）。不負責 SaveChanges。</summary>
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
                ["total_amount"] = FormatAmount(order.TotalAmount),
                ["currency"]     = order.Currency.ToUpperInvariant(),
                ["download_url"] = downloadUrl,
                ["purchased_at"] = (order.CompletedAt ?? DateTimeOffset.UtcNow)
                    .ToOffset(TaipeiOffset).ToString("yyyy/MM/dd HH:mm"),
                ["items_rows"]   = BuildItemsRows(order),
            },
            Locale: "zh-TW");
        outbox.Payload = JsonSerializer.Serialize(evt, OutboxJson.Options);
        db.OutboxMessages.Add(outbox);
    }

    /// <summary>最低貨幣單位轉面額顯示（兩位小數幣別；平台目前僅 TWD/USD 類）。</summary>
    private static string FormatAmount(long minorAmount) => (minorAmount / 100m).ToString("0.##");

    /// <summary>
    /// 逐項購買明細的預渲染 HTML 列（模板 <c>{{items_rows}}</c> 插槽）。EmailService 的模板引擎僅做
    /// 字串替換、不支援迴圈，故列 markup 在此組出，內聯樣式須與 order-completed 模板的明細框一致。
    /// 品名為使用者輸入，一律 HTML encode 防注入。
    /// </summary>
    private static string BuildItemsRows(Order order)
    {
        var currency = order.Currency.ToUpperInvariant();
        var sb = new StringBuilder();
        foreach (var item in order.Items)
        {
            sb.Append("<tr>")
              .Append("<td class=\"body-font\" style=\"font-size:13px; font-weight:500; line-height:1.8; color:#1A1A1A; padding:0 20px 6px 0;\">")
              .Append(WebUtility.HtmlEncode(item.CatalogName))
              .Append("</td>")
              .Append("<td class=\"display\" align=\"right\" style=\"font-size:13px; font-weight:700; line-height:1.8; color:#1A1A1A; padding:0 0 6px; white-space:nowrap;\">")
              .Append(FormatAmount(item.UnitPrice)).Append(' ').Append(currency)
              .Append("</td>")
              .Append("</tr>");
        }
        return sb.ToString();
    }
}
