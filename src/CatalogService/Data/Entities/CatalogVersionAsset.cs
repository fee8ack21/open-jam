using Shared.Audit;

namespace CatalogService.Data.Entities;

/// <summary>
/// 商品版本的可下載檔案（買家實際取得的數位內容）。
/// Id 與 StorageService 簽發的 FileId 相同值，皆為私有物件，須經授權才能簽發下載 URL。
/// </summary>
public class CatalogVersionAsset : ICreatedAt
{
    /// <summary>資產唯一識別碼，與 StorageService 簽發的 FileId 相同值。</summary>
    public Guid Id { get; set; }

    /// <summary>所屬商品版本 ID。</summary>
    public Guid CatalogVersionId { get; set; }

    /// <summary>使用者上傳時的原始檔名。</summary>
    public string FileName { get; set; } = "";

    /// <summary>在儲存後端的物件鍵值。</summary>
    public string StorageKey { get; set; } = "";

    /// <summary>檔案大小（bytes）。</summary>
    public long FileSize { get; set; }

    /// <summary>MIME 類型。</summary>
    public string ContentType { get; set; } = "";

    /// <summary>同版本內檔案的顯示排序（由小到大）。</summary>
    public int SortOrder { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; private set; }
}
