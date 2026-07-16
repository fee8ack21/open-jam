using Shared.Audit;

namespace CatalogService.Data.Entities;

/// <summary>數位商品（商品主檔）。每個商品隸屬於一間商店。僅未曾上架的草稿可刪除（軟刪除）。</summary>
public class Catalog : ICreatedAt, IUpdatedAt, IDeletedAt, IDeletedBy
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

    /// <summary>累計銷量（已完成訂單中此商品出現的次數）；由 OrderCompletedEvent 累加。</summary>
    public long SalesCount { get; set; }

    /// <summary>商品詳情頁累計瀏覽次數。</summary>
    public long ViewCount { get; set; }

    /// <summary>是否為編輯精選（平台策展）；由 Admin 設定，市集首頁精選輪播取用。</summary>
    public bool IsFeatured { get; set; }

    /// <summary>是否為店長精選；由商店 Owner 於自家店面標記，店面首頁 spotlight 取用。</summary>
    public bool IsStoreFeatured { get; set; }

    /// <summary>店長精選顯示排序（小者在前）；非精選商品此值無意義。設為精選時自動接續於現有精選之後，可由 Owner 重排。</summary>
    public int StoreFeaturedSortOrder { get; set; }

    /// <summary>平均評分（0–5）；由評論彙總，無評論時為 0。</summary>
    public double RatingAverage { get; set; }

    /// <summary>評論數。</summary>
    public int RatingCount { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <inheritdoc/>
    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>首次上架時間；null 表示尚未上架。</summary>
    public DateTimeOffset? PublishedAt { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset? DeletedAt { get; private set; }

    /// <inheritdoc/>
    public Guid? DeletedBy { get; private set; }
}
