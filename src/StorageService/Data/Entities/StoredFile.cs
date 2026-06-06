using Shared.Audit;

namespace StorageService.Data.Entities;

/// <summary>已上傳或處理中的數位商品檔案紀錄。</summary>
public class StoredFile : ICreatedAt, IUpdatedAt, IDeletedAt, IDeletedBy
{
    /// <summary>檔案唯一識別碼。</summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>擁有者（創作者）ID。</summary>
    public Guid CreatorId { get; set; }

    /// <summary>所屬商品 ID；null 表示尚未關聯商品。</summary>
    public Guid? ProductId { get; set; }

    /// <summary>在儲存後端（MinIO / GCS）的物件鍵值，格式：creators/{creatorId}/{fileId}/{originalName}。</summary>
    public string StorageKey { get; set; } = "";

    /// <summary>使用者上傳時的原始檔名。</summary>
    public string OriginalName { get; set; } = "";

    /// <summary>MIME 類型，例如 "video/mp4"、"image/jpeg"、"application/pdf"。</summary>
    public string ContentType { get; set; } = "";

    /// <summary>檔案大小（bytes）；上傳完成前為 null。</summary>
    public long? SizeBytes { get; set; }

    /// <summary>媒體類型分類。</summary>
    public FileType FileType { get; set; }

    /// <summary>目前處理狀態。</summary>
    public FileStatus Status { get; set; } = FileStatus.Uploading;

    /// <summary>是否為公開預覽衍生檔（試看片段、PDF 前幾頁等）。</summary>
    public bool IsPreview { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <inheritdoc/>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset? DeletedAt { get; set; }

    /// <inheritdoc/>
    public Guid? DeletedBy { get; set; }

    /// <summary>軟刪除此檔案；已購買的商品仍保留買家下載權。</summary>
    public void SoftDelete(Guid? deletedBy = null)
    {
        DeletedAt = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy;
    }
}

/// <summary>媒體類型分類。</summary>
public enum FileType
{
    /// <summary>影片（支援 HLS 轉碼）。</summary>
    Video,

    /// <summary>圖片（支援縮圖生成）。</summary>
    Image,

    /// <summary>PDF 文件（支援預覽頁生成）。</summary>
    Pdf,
}

/// <summary>檔案處理狀態。</summary>
public enum FileStatus
{
    /// <summary>已簽發上傳 URL，等待直傳完成。</summary>
    Uploading,

    /// <summary>Storage 已收到上傳通知，正在進行掃毒 / 轉碼 / 預覽生成。</summary>
    Processing,

    /// <summary>所有處理完成，可對外提供下載。</summary>
    Ready,

    /// <summary>處理失敗（掃毒不通過 / 轉碼錯誤）或上傳逾時未確認。</summary>
    Failed,
}
