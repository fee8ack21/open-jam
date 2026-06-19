using Shared.Audit;

namespace CatalogService.Data.Entities;

/// <summary>數位商品（商品主檔）。每個商品隸屬於一間商店。</summary>
public class Catalog : ICreatedAt, IUpdatedAt
{
    /// <summary>商品唯一識別碼。</summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>所屬商店 ID（StoreService 的 Store）。</summary>
    public Guid StoreId { get; set; }

    /// <summary>目前對外提供下載的版本 ID；null 表示尚無任何版本。</summary>
    public Guid? CurrentVersionId { get; set; }

    /// <summary>所屬分類 ID（平台維護的 CatalogCategory）；null 表示未分類。</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>商品名稱。</summary>
    public string Name { get; set; } = "";

    /// <summary>商品代稱，於同一商店內唯一，用於組合商品頁網址。</summary>
    public string Slug { get; set; } = "";

    /// <summary>商品描述。</summary>
    public string? Description { get; set; }

    /// <summary>一句話簡介（市集卡片用的短標語）；null 表示未設定。</summary>
    public string? Summary { get; set; }

    /// <summary>封面色相（0–359），無縮圖時用於生成漸層佔位封面，亦為跨站視覺識別色。</summary>
    public int CoverHue { get; set; } = 256;

    /// <summary>商品狀態。</summary>
    public CatalogStatus Status { get; set; } = CatalogStatus.Draft;

    /// <summary>縮圖 Asset ID（對應某筆 Thumbnail 類型的 CatalogAsset）；null 表示未設定。</summary>
    public Guid? ThumbnailAssetId { get; set; }

    /// <summary>售價。</summary>
    public decimal Price { get; set; }

    /// <summary>幣別（ISO 4217，例如 TWD）。</summary>
    public string Currency { get; set; } = "TWD";

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <inheritdoc/>
    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>首次上架時間；null 表示尚未上架。</summary>
    public DateTimeOffset? PublishedAt { get; set; }
}
