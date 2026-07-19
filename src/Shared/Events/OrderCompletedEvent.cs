namespace Shared.Events;

/// <summary>
/// 訂單履約完成事件，由 OrderService 在訂單轉為 Completed 的業務 transaction 內寫入 Outbox，
/// 再由排程搬入 RabbitMQ。攜帶商品明細（fat event），消費者無需反查 OrderService。
/// CatalogService 消費以累加商品銷量（未來亦用於發放下載權限）。
/// </summary>
public record OrderCompletedEvent(
    /// <summary>Outbox 訊息 ID，作為冪等去重鍵。</summary>
    Guid OutboxMessageId,
    /// <summary>訂單 ID。</summary>
    Guid OrderId,
    /// <summary>訂單所屬商店 ID。</summary>
    Guid StoreId,
    /// <summary>訂單完成（付款成功）時間（UTC）。</summary>
    DateTimeOffset CompletedAt,
    /// <summary>訂單項目明細。</summary>
    IReadOnlyList<OrderCompletedItem> Items,
    /// <summary>買家使用者 ID；訪客（免註冊）訂單為 null。</summary>
    Guid? BuyerUserId,
    /// <summary>買家 Email（結帳時填寫）。NotificationService 據此維護商品買家參照表。</summary>
    string BuyerEmail
);

/// <summary>訂單完成事件中的單一商品項目。</summary>
public record OrderCompletedItem(
    /// <summary>商品 ID。</summary>
    Guid CatalogId,
    /// <summary>購買的商品版本 ID。</summary>
    Guid CatalogVersionId
);
