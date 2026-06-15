using Shared.Audit;

namespace StoreService.Data.Entities;

/// <summary>
/// 已上傳檔案的落地紀錄。Id 與 StorageService 簽發的 FileId 相同值。
/// 獨立表，不對 StorageService 的 StoredFiles 做反向查詢或 FK。
/// </summary>
public class Asset : ICreatedAt
{
    /// <summary>資產唯一識別碼，與 StorageService 簽發的 FileId 相同值。</summary>
    public Guid Id { get; set; }

    /// <summary>上傳者使用者 ID。</summary>
    public Guid CreatedBy { get; set; }

    /// <summary>在儲存後端（本地檔案 / GCS）的物件鍵值。</summary>
    public string StorageKey { get; set; } = "";

    /// <summary>使用者上傳時的原始檔名。</summary>
    public string FileName { get; set; } = "";

    /// <summary>MIME 類型。</summary>
    public string ContentType { get; set; } = "";

    /// <summary>檔案大小（bytes）。</summary>
    public long FileSize { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; private set; }
}
