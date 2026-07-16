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

    /// <summary>功能 API 確認此檔已被實際使用（掛上商品資產等 reference）的時間；null 表示尚未被使用。</summary>
    public DateTimeOffset? ReferencedAt { get; set; }

    /// <summary>在儲存後端（本地檔案 / GCS）的物件鍵值，格式：creators/{creatorId}/{fileId}/{originalName}。</summary>
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

    /// <summary>是否為公開讀取物件（例如商店 Avatar/Banner）；決定儲存後端的公開 / 私有隔離（GCS bucket、本地匿名讀取）。</summary>
    public bool IsPublic { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <inheritdoc/>
    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <inheritdoc/>
    public DateTimeOffset? DeletedAt { get; private set; }

    /// <inheritdoc/>
    public Guid? DeletedBy { get; private set; }
}
