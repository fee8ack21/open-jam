using Shared.Audit;

namespace OrderService.Data.Entities;

/// <summary>商品訂單，對應一次結帳。一張訂單可含多個 <see cref="OrderItem"/>。</summary>
public class Order : ICreatedAt, IUpdatedAt
{
    public Guid Id { get; set; }

    /// <summary>對外顯示的人類可讀訂單編號（如 OJ-20260624-1A2B3C4D），全域唯一。</summary>
    public string OrderNumber { get; set; } = "";

    /// <summary>訂單所屬商店 ID（賣方）。一張訂單對應單一商店，供賣家視角查詢用。</summary>
    public Guid StoreId { get; set; }

    /// <summary>購買者使用者 ID（已登入用戶）；null 表示以 Email 匿名購買。</summary>
    public Guid? BuyerUserId { get; set; }

    /// <summary>購買者電子信箱。</summary>
    public string BuyerEmail { get; set; } = "";

    /// <summary>訂單狀態。</summary>
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    /// <summary>貨幣代碼（小寫，如 "usd"、"twd"）。</summary>
    public string Currency { get; set; } = "usd";

    /// <summary>訂單總金額（最低貨幣單位，如 cents），為各項目單價之和。</summary>
    public long TotalAmount { get; set; }

    /// <summary>平台抽成金額（最低貨幣單位，付款成功時由 PaymentSucceededEvent 快照）。
    /// 免費訂單為 0；本欄位上線前完成的舊付費訂單亦為 0（未回填）。賣家實收 = TotalAmount − 本值。</summary>
    public long PlatformFeeAmount { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <inheritdoc/>
    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>訂單履約完成時間（UTC）；null 表示尚未完成。</summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>訂單項目。</summary>
    public List<OrderItem> Items { get; set; } = [];

    /// <summary>訂單狀態變更歷程。</summary>
    public List<OrderStatusHistory> StatusHistory { get; set; } = [];
}
