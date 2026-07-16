using Shared.Audit;

namespace CatalogService.Data.Entities;

/// <summary>
/// 商品的展示型資產（縮圖、截圖、預覽影音、外部影片嵌入）。
/// 實體檔案資產的 Id 與 StorageService 簽發的 FileId 相同值，皆為公開讀取物件；
/// 外部嵌入（ExternalVideo）無儲存端檔案，Id 為新產生的 Guid、storage 欄位留空。
/// </summary>
public class CatalogAsset : ICreatedAt
{
    /// <summary>資產唯一識別碼；實體檔案資產與 StorageService 簽發的 FileId 相同值。</summary>
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

    /// <summary>顯示排序（由小到大）；預覽媒體（截圖 / 預覽影音 / 外部嵌入）共用同一條序列。</summary>
    public int SortOrder { get; set; }

    /// <summary>外部影片嵌入 URL（僅 ExternalVideo 型別有值）。</summary>
    public string? ExternalUrl { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; private set; }
}
