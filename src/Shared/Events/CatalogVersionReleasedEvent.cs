namespace Shared.Events;

/// <summary>
/// 商品新版本發布通知事件，由 CatalogService 在創作者對已上架商品的版本按下「通知買家」時
/// 寫入 Outbox，再由排程搬入 RabbitMQ。攜帶通知渲染所需欄位（fat event），消費者無需反查
/// CatalogService。NotificationService 消費以通知已購買該商品的買家（每版本至多觸發一次）。
/// </summary>
public record CatalogVersionReleasedEvent(
    /// <summary>Outbox 訊息 ID，作為冪等去重鍵。</summary>
    Guid OutboxMessageId,
    /// <summary>商品 ID。</summary>
    Guid CatalogId,
    /// <summary>商品所屬商店 ID。</summary>
    Guid StoreId,
    /// <summary>商品名稱。</summary>
    string CatalogName,
    /// <summary>商品代稱（同一商店內唯一，用於組合商品頁網址）。</summary>
    string CatalogSlug,
    /// <summary>版本 ID。</summary>
    Guid VersionId,
    /// <summary>版本名稱（如 1.1.0）。</summary>
    string Version,
    /// <summary>版本更新說明；無則為 null。</summary>
    string? ReleaseNote,
    /// <summary>通知觸發時間（UTC）。</summary>
    DateTimeOffset ReleasedAt
);
