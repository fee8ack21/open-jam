namespace StoreService.Models;

/// <summary>申請 Avatar/Banner 上傳簽章 URL 請求。</summary>
public class RequestAssetUploadUrlRequest
{
    /// <summary>原始檔名（含副檔名）。</summary>
    /// <example>avatar.png</example>
    public string FileName { get; set; } = "";

    /// <summary>MIME 類型，僅允許 jpeg/png/gif/webp。</summary>
    /// <example>image/png</example>
    public string ContentType { get; set; } = "";

    /// <summary>檔案大小（bytes）。</summary>
    /// <example>204800</example>
    public long SizeBytes { get; set; }
}
