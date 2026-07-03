namespace Shared.Events;

/// <summary>
/// 商品上架事件，由 CatalogService 在商品轉為 Published 的業務 transaction 內寫入 Outbox，
/// 再由排程搬入 RabbitMQ。攜帶通知渲染所需欄位（fat event），消費者無需反查 CatalogService。
/// NotificationService 消費以通知追蹤該商店的追蹤者（僅首次上架）。
/// </summary>
public record CatalogPublishedEvent(
    /// <summary>Outbox 訊息 ID，作為冪等去重鍵。</summary>
    Guid OutboxMessageId,
    /// <summary>商品 ID。</summary>
    Guid CatalogId,
    /// <summary>商品所屬商店 ID。</summary>
    Guid StoreId,
    /// <summary>商品名稱。</summary>
    string Name,
    /// <summary>商品代稱（同一商店內唯一，用於組合商品頁網址）。</summary>
    string Slug,
    /// <summary>售價。</summary>
    decimal Price,
    /// <summary>幣別（ISO 4217，例如 TWD）。</summary>
    string Currency,
    /// <summary>上架時間（UTC）。</summary>
    DateTimeOffset PublishedAt,
    /// <summary>是否為首次上架；重新上架（Archived → Published）為 false，消費者據此決定是否通知。</summary>
    bool IsFirstPublish
);
