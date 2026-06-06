using StorageService.Data.Entities;

namespace StorageService.Models;

/// <summary>檔案紀錄回應 DTO。</summary>
public class FileDto
{
    /// <summary>檔案唯一識別碼。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid Id { get; set; }

    /// <summary>擁有者（創作者）ID。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid CreatorId { get; set; }

    /// <summary>所屬商品 ID。</summary>
    public Guid? ProductId { get; set; }

    /// <summary>使用者上傳時的原始檔名。</summary>
    /// <example>intro-video.mp4</example>
    public string OriginalName { get; set; } = "";

    /// <summary>MIME 類型。</summary>
    /// <example>video/mp4</example>
    public string ContentType { get; set; } = "";

    /// <summary>檔案大小（bytes）。</summary>
    /// <example>104857600</example>
    public long? SizeBytes { get; set; }

    /// <summary>媒體類型分類。</summary>
    /// <example>Video</example>
    public FileType FileType { get; set; }

    /// <summary>目前處理狀態。</summary>
    /// <example>Ready</example>
    public FileStatus Status { get; set; }

    /// <summary>是否為公開預覽衍生檔。</summary>
    /// <example>false</example>
    public bool IsPreview { get; set; }

    /// <summary>建立時間（UTC）。</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>最後更新時間（UTC）。</summary>
    public DateTimeOffset? UpdatedAt { get; set; }
}
