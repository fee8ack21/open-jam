using Shared.Audit;

namespace NotificationService.Data.Entities;

/// <summary>
/// 商品買家參照表（微服務 Ref Table）。來源為 OrderService 的已完成訂單，
/// 由 OrderCompletedEvent 同步（每商品每信箱一筆，OrderId 保留最新一筆完成訂單）；
/// 信箱一律正規化小寫。供 catalog.version_released 通知 fan-out 至既有買家。
/// </summary>
public class CatalogBuyerRef : ICreatedAt
{
    /// <summary>參照紀錄唯一識別碼。</summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>商品 ID。</summary>
    public Guid CatalogId { get; set; }

    /// <summary>商品所屬商店 ID。</summary>
    public Guid StoreId { get; set; }

    /// <summary>買家電子信箱（小寫）。</summary>
    public string Email { get; set; } = "";

    /// <summary>買家使用者 ID；null 表示訪客憑信箱購買，註冊後由 UserRegisteredEvent 回填。</summary>
    public Guid? UserId { get; set; }

    /// <summary>最新一筆包含此商品的完成訂單 ID，作為信中下載頁連結的憑證。</summary>
    public Guid OrderId { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; private set; }
}
