using System.Text.Json;

namespace NotificationService.Models;

/// <summary>通知類型常數（NotificationRequest.Type / Notification.Type）。</summary>
public static class NotificationTypes
{
    /// <summary>追蹤商店首次上架商品。</summary>
    public const string CatalogPublished = "catalog.published";

    /// <summary>商店公告（創作者自訂內容，可預定發送時間）。</summary>
    public const string StoreAnnouncement = "store.announcement";

    /// <summary>已購商品發布新版本（fan-out 對象為該商品的既有買家，非商店追蹤者）。</summary>
    public const string CatalogVersionReleased = "catalog.version_released";
}

/// <summary>通知 Payload 的 JSON 序列化設定，camelCase 便於前端直接取用。</summary>
public static class PayloadJson
{
    /// <summary>Payload 序列化 / 反序列化共用設定。</summary>
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
}

/// <summary>catalog.published 通知的內容參數。</summary>
public class CatalogPublishedPayload
{
    /// <summary>商品 ID。</summary>
    public Guid CatalogId { get; set; }

    /// <summary>商品名稱。</summary>
    public string CatalogName { get; set; } = "";

    /// <summary>商品代稱。</summary>
    public string CatalogSlug { get; set; } = "";

    /// <summary>售價。</summary>
    public decimal Price { get; set; }

    /// <summary>幣別（ISO 4217）。</summary>
    public string Currency { get; set; } = "";

    /// <summary>商店名稱；由 dispatcher 發送時補上。</summary>
    public string? StoreName { get; set; }

    /// <summary>商店子網域代稱；由 dispatcher 發送時補上。</summary>
    public string? StoreSlug { get; set; }
}

/// <summary>catalog.version_released 通知的內容參數。</summary>
public class CatalogVersionReleasedPayload
{
    /// <summary>商品 ID。</summary>
    public Guid CatalogId { get; set; }

    /// <summary>商品名稱。</summary>
    public string CatalogName { get; set; } = "";

    /// <summary>商品代稱。</summary>
    public string CatalogSlug { get; set; } = "";

    /// <summary>版本 ID。</summary>
    public Guid VersionId { get; set; }

    /// <summary>版本名稱（如 1.1.0）。</summary>
    public string Version { get; set; } = "";

    /// <summary>版本更新說明；無則為 null。</summary>
    public string? ReleaseNote { get; set; }

    /// <summary>商店名稱；由 dispatcher 發送時補上。</summary>
    public string? StoreName { get; set; }

    /// <summary>商店子網域代稱；由 dispatcher 發送時補上。</summary>
    public string? StoreSlug { get; set; }
}

/// <summary>store.announcement 通知的內容參數。</summary>
public class StoreAnnouncementPayload
{
    /// <summary>公告標題。</summary>
    public string Title { get; set; } = "";

    /// <summary>公告內文。</summary>
    public string Message { get; set; } = "";

    /// <summary>商店名稱；由 dispatcher 發送時補上。</summary>
    public string? StoreName { get; set; }

    /// <summary>商店子網域代稱；由 dispatcher 發送時補上。</summary>
    public string? StoreSlug { get; set; }
}
