using CatalogService.Data.Entities;

namespace CatalogService.Models;

/// <summary>商品完整資訊回應（商品詳情頁）。</summary>
public class CatalogDto
{
    /// <summary>商品唯一識別碼。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid Id { get; set; }

    /// <summary>所屬商店 ID。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid StoreId { get; set; }

    /// <summary>所屬分類 ID；null 表示未分類。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid? CategoryId { get; set; }

    /// <summary>商品名稱。</summary>
    /// <example>像素風格音效包</example>
    public string Name { get; set; } = "";

    /// <summary>商品代稱（同商店內唯一）。</summary>
    /// <example>pixel-sfx-pack</example>
    public string Slug { get; set; } = "";

    /// <summary>商品描述。</summary>
    /// <example>200 個復古像素遊戲音效，WAV 格式。</example>
    public string? Description { get; set; }

    /// <summary>一句話簡介（市集卡片短標語）；null 表示未設定。</summary>
    /// <example>復古像素遊戲必備的 8-bit 音效合輯。</example>
    public string? Summary { get; set; }

    /// <summary>封面色相（0–359），無縮圖時生成漸層佔位封面。</summary>
    /// <example>256</example>
    public int CoverHue { get; set; }

    /// <summary>商品狀態。</summary>
    /// <example>Published</example>
    public CatalogStatus Status { get; set; }

    /// <summary>售價。</summary>
    /// <example>150.00</example>
    public decimal Price { get; set; }

    /// <summary>幣別（ISO 4217）。</summary>
    /// <example>TWD</example>
    public string Currency { get; set; } = "";

    /// <summary>累計銷量。</summary>
    /// <example>0</example>
    public long SalesCount { get; set; }

    /// <summary>是否為編輯精選（平台策展）。</summary>
    /// <example>false</example>
    public bool IsFeatured { get; set; }

    /// <summary>平均評分（0–5）；無評論時為 0。</summary>
    /// <example>4.6</example>
    public double RatingAverage { get; set; }

    /// <summary>評論數。</summary>
    /// <example>128</example>
    public int RatingCount { get; set; }

    /// <summary>縮圖公開 URL；null 表示尚未設定。</summary>
    /// <example>http://localhost:5171/v1/files/blob/public/.../thumb.png</example>
    public string? ThumbnailUrl { get; set; }

    /// <summary>目前對外版本；null 表示尚無版本。</summary>
    public CatalogVersionDto? CurrentVersion { get; set; }

    /// <summary>展示型資產（縮圖 / 截圖 / 預覽影音）清單。</summary>
    public List<CatalogAssetDto> Assets { get; set; } = [];

    /// <summary>標籤名稱清單。</summary>
    /// <example>["audio","retro","8bit"]</example>
    public List<string> Tags { get; set; } = [];

    /// <summary>首次上架時間；null 表示尚未上架。</summary>
    public DateTimeOffset? PublishedAt { get; set; }

    /// <summary>建立時間。</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>最後更新時間。</summary>
    public DateTimeOffset? UpdatedAt { get; set; }
}

/// <summary>商品列表項目（精簡欄位）。</summary>
public class CatalogSummaryDto
{
    /// <summary>商品唯一識別碼。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid Id { get; set; }

    /// <summary>所屬商店 ID。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid StoreId { get; set; }

    /// <summary>所屬分類 ID；null 表示未分類。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid? CategoryId { get; set; }

    /// <summary>商品名稱。</summary>
    /// <example>像素風格音效包</example>
    public string Name { get; set; } = "";

    /// <summary>商品代稱。</summary>
    /// <example>pixel-sfx-pack</example>
    public string Slug { get; set; } = "";

    /// <summary>一句話簡介（市集卡片短標語）；null 表示未設定。</summary>
    /// <example>復古像素遊戲必備的 8-bit 音效合輯。</example>
    public string? Summary { get; set; }

    /// <summary>封面色相（0–359），無縮圖時生成漸層佔位封面。</summary>
    /// <example>256</example>
    public int CoverHue { get; set; }

    /// <summary>售價。</summary>
    /// <example>150.00</example>
    public decimal Price { get; set; }

    /// <summary>幣別（ISO 4217）。</summary>
    /// <example>TWD</example>
    public string Currency { get; set; } = "";

    /// <summary>商品狀態。</summary>
    /// <example>Published</example>
    public CatalogStatus Status { get; set; }

    /// <summary>累計銷量。</summary>
    /// <example>0</example>
    public long SalesCount { get; set; }

    /// <summary>是否為編輯精選（平台策展）。</summary>
    /// <example>false</example>
    public bool IsFeatured { get; set; }

    /// <summary>平均評分（0–5）；無評論時為 0。</summary>
    /// <example>4.6</example>
    public double RatingAverage { get; set; }

    /// <summary>評論數。</summary>
    /// <example>128</example>
    public int RatingCount { get; set; }

    /// <summary>縮圖公開 URL；null 表示尚未設定。</summary>
    /// <example>http://localhost:5171/v1/files/blob/public/.../thumb.png</example>
    public string? ThumbnailUrl { get; set; }

    /// <summary>標籤名稱清單（市集卡片標籤、標籤搜尋用）。</summary>
    /// <example>["audio","retro","8bit"]</example>
    public List<string> Tags { get; set; } = [];

    /// <summary>首次上架時間；null 表示尚未上架。</summary>
    public DateTimeOffset? PublishedAt { get; set; }
}

/// <summary>展示型資產回應。</summary>
public class CatalogAssetDto
{
    /// <summary>資產唯一識別碼。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid Id { get; set; }

    /// <summary>資產類型。</summary>
    /// <example>Screenshot</example>
    public CatalogAssetType Type { get; set; }

    /// <summary>原始檔名。</summary>
    /// <example>screenshot-1.png</example>
    public string FileName { get; set; } = "";

    /// <summary>MIME 類型。</summary>
    /// <example>image/png</example>
    public string ContentType { get; set; } = "";

    /// <summary>檔案大小（bytes）。</summary>
    /// <example>204800</example>
    public long FileSize { get; set; }

    /// <summary>同類型內顯示排序。</summary>
    /// <example>0</example>
    public int SortOrder { get; set; }

    /// <summary>公開讀取 URL。</summary>
    /// <example>http://localhost:5171/v1/files/blob/public/.../screenshot-1.png</example>
    public string Url { get; set; } = "";

    /// <summary>建立時間。</summary>
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>建立商品請求。</summary>
public class CreateCatalogRequest
{
    /// <summary>所屬商店 ID。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid StoreId { get; set; }

    /// <summary>商品名稱（1–200 字）。</summary>
    /// <example>像素風格音效包</example>
    public string Name { get; set; } = "";

    /// <summary>商品代稱（同商店內唯一，3–100 字小寫英數字與連字號）。</summary>
    /// <example>pixel-sfx-pack</example>
    public string Slug { get; set; } = "";

    /// <summary>商品描述。</summary>
    /// <example>200 個復古像素遊戲音效，WAV 格式。</example>
    public string? Description { get; set; }

    /// <summary>一句話簡介（市集卡片短標語，至多 200 字）；null 表示未設定。</summary>
    /// <example>復古像素遊戲必備的 8-bit 音效合輯。</example>
    public string? Summary { get; set; }

    /// <summary>封面色相（0–359）；省略時預設 256。</summary>
    /// <example>256</example>
    public int? CoverHue { get; set; }

    /// <summary>所屬分類 ID；null 表示未分類。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid? CategoryId { get; set; }

    /// <summary>售價（&gt;= 0）。</summary>
    /// <example>150.00</example>
    public decimal Price { get; set; }

    /// <summary>幣別（ISO 4217）；省略時預設 TWD。</summary>
    /// <example>TWD</example>
    public string? Currency { get; set; }

    /// <summary>初始標籤名稱清單（強制小寫）。</summary>
    /// <example>["audio","retro"]</example>
    public List<string>? Tags { get; set; }
}

/// <summary>更新商品請求（部分欄位，null 表示不變更）。</summary>
public class UpdateCatalogRequest
{
    /// <summary>商品名稱；null 表示不變更。</summary>
    /// <example>像素風格音效包 Vol.2</example>
    public string? Name { get; set; }

    /// <summary>商品代稱；null 表示不變更。</summary>
    /// <example>pixel-sfx-pack-2</example>
    public string? Slug { get; set; }

    /// <summary>商品描述；null 表示不變更，空字串表示清空。</summary>
    /// <example>新增 50 個音效。</example>
    public string? Description { get; set; }

    /// <summary>一句話簡介；null 表示不變更，空字串表示清空。</summary>
    /// <example>全新擴充版，收錄 250 個音效。</example>
    public string? Summary { get; set; }

    /// <summary>封面色相（0–359）；null 表示不變更。</summary>
    /// <example>320</example>
    public int? CoverHue { get; set; }

    /// <summary>售價；null 表示不變更。</summary>
    /// <example>180.00</example>
    public decimal? Price { get; set; }

    /// <summary>幣別（ISO 4217）；null 表示不變更。</summary>
    /// <example>TWD</example>
    public string? Currency { get; set; }
}

/// <summary>設定商品分類請求。</summary>
public class SetCatalogCategoryRequest
{
    /// <summary>分類 ID；null 表示移除分類。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid? CategoryId { get; set; }
}

/// <summary>設定商品標籤請求（全量覆蓋）。</summary>
public class SetCatalogTagsRequest
{
    /// <summary>標籤名稱清單（強制小寫，去重）。空陣列表示清除所有標籤。</summary>
    /// <example>["audio","retro","8bit"]</example>
    public List<string> Tags { get; set; } = [];
}

/// <summary>商品列表排序方式。</summary>
public enum CatalogSort
{
    /// <summary>最新上架（依上架時間，未上架者依建立時間）由新到舊——預設。</summary>
    Newest = 0,

    /// <summary>價格由低到高。</summary>
    PriceLowToHigh = 1,

    /// <summary>價格由高到低。</summary>
    PriceHighToLow = 2,
}

/// <summary>商品列表查詢請求（分頁採 offset / limit）。</summary>
public class ListCatalogsRequest
{
    /// <summary>限定商店 ID；null 表示不限。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid? StoreId { get; set; }

    /// <summary>限定分類 ID；null 表示不限。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid? CategoryId { get; set; }

    /// <summary>限定標籤名稱；null 表示不限。</summary>
    /// <example>retro</example>
    public string? Tag { get; set; }

    /// <summary>名稱關鍵字搜尋；null 表示不限。</summary>
    /// <example>音效</example>
    public string? Search { get; set; }

    /// <summary>僅限編輯精選；true 只回精選、false 只回非精選、null 表示不限。</summary>
    /// <example>true</example>
    public bool? Featured { get; set; }

    /// <summary>售價下限（含）；null 表示不限。</summary>
    /// <example>0</example>
    public decimal? MinPrice { get; set; }

    /// <summary>售價上限（含）；null 表示不限。</summary>
    /// <example>30</example>
    public decimal? MaxPrice { get; set; }

    /// <summary>排序方式；省略時預設最新上架。</summary>
    /// <example>Newest</example>
    public CatalogSort Sort { get; set; } = CatalogSort.Newest;

    /// <summary>略過筆數。</summary>
    /// <example>0</example>
    public int Offset { get; set; } = 0;

    /// <summary>每頁筆數（最大 100）。</summary>
    /// <example>20</example>
    public int Limit { get; set; } = 20;
}

/// <summary>商品列表分頁回應。</summary>
public class ListCatalogsResponse
{
    /// <summary>符合條件的總筆數（未分頁）。</summary>
    /// <example>42</example>
    public int TotalCount { get; set; }

    /// <summary>本頁商品清單。</summary>
    public List<CatalogSummaryDto> Items { get; set; } = [];
}

/// <summary>申請展示型資產上傳簽章 URL 請求。</summary>
public class RequestCatalogAssetUploadUrlRequest
{
    /// <summary>資產類型。</summary>
    /// <example>Screenshot</example>
    public CatalogAssetType Type { get; set; }

    /// <summary>原始檔名（含副檔名）。</summary>
    /// <example>screenshot-1.png</example>
    public string FileName { get; set; } = "";

    /// <summary>MIME 類型。</summary>
    /// <example>image/png</example>
    public string ContentType { get; set; } = "";

    /// <summary>檔案大小（bytes）。</summary>
    /// <example>204800</example>
    public long SizeBytes { get; set; }
}

/// <summary>展示型資產上傳簽章 URL 回應。</summary>
public class CatalogAssetUploadUrlResponse
{
    /// <summary>已建立的 Asset ID。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid AssetId { get; set; }

    /// <summary>前端應使用此 URL 以 HTTP PUT 直傳檔案。</summary>
    /// <example>http://localhost:5171/v1/files/blob/public/.../screenshot-1.png?expires=1735689600&amp;sig=...</example>
    public string UploadUrl { get; set; } = "";

    /// <summary>上傳完成後的公開讀取網址。</summary>
    /// <example>http://localhost:5171/v1/files/blob/public/.../screenshot-1.png</example>
    public string PublicUrl { get; set; } = "";

    /// <summary>簽章 URL 過期時間（UTC）。</summary>
    public DateTimeOffset ExpiresAt { get; set; }
}
