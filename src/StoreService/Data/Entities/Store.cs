using Shared.Audit;

namespace StoreService.Data.Entities;

/// <summary>創作者商店。</summary>
public class Store : ICreatedAt, IUpdatedAt
{
    /// <summary>商店唯一識別碼。</summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>顯示名稱。</summary>
    public string StoreName { get; set; } = "";

    /// <summary>子網域代稱（`&lt;slug&gt;.openjam.co`），全域唯一。</summary>
    public string StoreSlug { get; set; } = "";

    /// <summary>商店描述。</summary>
    public string? Description { get; set; }

    /// <summary>頭像圖片 Asset ID。</summary>
    public Guid? AvatarAssetId { get; set; }

    /// <summary>橫幅圖片 Asset ID。</summary>
    public Guid? BannerAssetId { get; set; }

    /// <summary>商店狀態。</summary>
    public StoreStatus Status { get; set; } = StoreStatus.Active;

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <inheritdoc/>
    public DateTimeOffset? UpdatedAt { get; private set; }
}
