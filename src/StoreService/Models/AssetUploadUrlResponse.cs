namespace StoreService.Models;

/// <summary>Avatar/Banner 上傳簽章 URL 回應。</summary>
public class AssetUploadUrlResponse
{
    /// <summary>已建立的 Asset ID。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid AssetId { get; set; }

    /// <summary>前端應使用此 URL 以 HTTP PUT 直傳檔案。</summary>
    /// <example>http://localhost:9000/open-jam/public/.../avatar.png?X-Amz-Signature=...</example>
    public string UploadUrl { get; set; } = "";

    /// <summary>上傳完成後的公開讀取網址。</summary>
    /// <example>http://localhost:9000/open-jam/public/.../avatar.png</example>
    public string PublicUrl { get; set; } = "";

    /// <summary>簽章 URL 過期時間（UTC）。</summary>
    public DateTimeOffset ExpiresAt { get; set; }
}
