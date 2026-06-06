using StorageService.Data.Entities;

namespace StorageService.Models;

/// <summary>向 StorageService 申請上傳簽章 URL 的請求體。</summary>
public class RequestUploadUrlRequest
{
    /// <summary>擁有者（創作者）ID。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid CreatorId { get; set; }

    /// <summary>所屬商品 ID；null 表示尚未關聯商品（暫存後再關聯）。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid? ProductId { get; set; }

    /// <summary>使用者上傳時的原始檔名（含副檔名）。</summary>
    /// <example>intro-video.mp4</example>
    public string OriginalName { get; set; } = "";

    /// <summary>MIME 類型；StorageService 驗證是否為允許格式。</summary>
    /// <example>video/mp4</example>
    public string ContentType { get; set; } = "";

    /// <summary>檔案大小（bytes）；用於配額檢查與 presigned URL 條件。</summary>
    /// <example>104857600</example>
    public long SizeBytes { get; set; }

    /// <summary>媒體類型分類。</summary>
    /// <example>Video</example>
    public FileType FileType { get; set; }

    /// <summary>是否為公開預覽衍生檔。</summary>
    /// <example>false</example>
    public bool IsPreview { get; set; }
}
