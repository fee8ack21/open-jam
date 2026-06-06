namespace StorageService.Models;

/// <summary>下載簽章 URL 回應。</summary>
public class GetDownloadUrlResponse
{
    /// <summary>檔案 ID。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid FileId { get; set; }

    /// <summary>有效的短效下載 URL；調用方應立即轉交給終端使用者，不應快取。</summary>
    /// <example>http://localhost:9000/open-jam/creators/.../intro-video.mp4?X-Amz-Signature=...</example>
    public string DownloadUrl { get; set; } = "";

    /// <summary>簽章 URL 過期時間（UTC）。</summary>
    public DateTimeOffset ExpiresAt { get; set; }
}
