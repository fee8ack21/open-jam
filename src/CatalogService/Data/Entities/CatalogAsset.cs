using Shared.Audit;

namespace CatalogService.Data.Entities;

/// <summary>
/// 商品的展示型資產（縮圖、截圖、預覽影音）。
/// Id 與 StorageService 簽發的 FileId 相同值，皆為公開讀取物件。
/// </summary>
public class CatalogAsset : ICreatedAt
{
    /// <summary>資產唯一識別碼，與 StorageService 簽發的 FileId 相同值。</summary>
    public Guid Id { get; set; }

    /// <summary>所屬商品 ID。</summary>
    public Guid CatalogId { get; set; }

    /// <summary>資產類型。</summary>
    public CatalogAssetType Type { get; set; }

    /// <summary>使用者上傳時的原始檔名。</summary>
    public string FileName { get; set; } = "";

    /// <summary>在儲存後端的物件鍵值。</summary>
    public string StorageKey { get; set; } = "";

    /// <summary>檔案大小（bytes）。</summary>
    public long FileSize { get; set; }

    /// <summary>MIME 類型。</summary>
    public string ContentType { get; set; } = "";

    /// <summary>同類型資產的顯示排序（由小到大）。</summary>
    public int SortOrder { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; private set; }
}
