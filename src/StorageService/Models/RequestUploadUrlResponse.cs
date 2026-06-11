namespace StorageService.Models;

/// <summary>上傳簽章 URL 回應。</summary>
public class RequestUploadUrlResponse
{
    /// <summary>已建立的檔案紀錄 ID；上傳完成後以此 ID 通知 storage-event。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid FileId { get; set; }

    /// <summary>前端應使用此 URL 以 HTTP PUT 直傳檔案，不得經過 API Server 轉傳。</summary>
    /// <example>http://localhost:9000/open-jam/creators/.../intro-video.mp4?X-Amz-Signature=...</example>
    public string UploadUrl { get; set; } = "";

    /// <summary>在儲存後端的物件鍵值。</summary>
    /// <example>public/3fa85f64-5717-4562-b3fc-2c963f66afa6/.../avatar.png</example>
    public string StorageKey { get; set; } = "";

    /// <summary>公開讀取網址；僅 `IsPublic=true` 時提供。</summary>
    /// <example>http://localhost:9000/open-jam/public/3fa85f64-5717-4562-b3fc-2c963f66afa6/.../avatar.png</example>
    public string? PublicUrl { get; set; }

    /// <summary>簽章 URL 過期時間（UTC）。</summary>
    public DateTimeOffset ExpiresAt { get; set; }
}
