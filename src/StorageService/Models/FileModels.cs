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

    /// <summary>配額預扣紀錄 ID（QuotaService）；功能 API 預扣後帶入，隨 FileReadyEvent 回帶供 commit；null 表示未經配額預扣。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid? ReservationId { get; set; }

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

    /// <summary>是否為公開讀取物件（例如商店 Avatar/Banner）；true 時物件鍵值前綴為 "public/"。</summary>
    /// <example>false</example>
    public bool IsPublic { get; set; }
}

/// <summary>上傳簽章 URL 回應。</summary>
public class RequestUploadUrlResponse
{
    /// <summary>已建立的檔案紀錄 ID；上傳完成後以此 ID 通知 storage-event。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid FileId { get; set; }

    /// <summary>前端應使用此 URL 以 HTTP PUT 直傳檔案，不得經過 API Server 轉傳。</summary>
    /// <example>http://localhost:5171/v1/files/blob/creators/.../intro-video.mp4?expires=1735689600&amp;sig=...</example>
    public string UploadUrl { get; set; } = "";

    /// <summary>在儲存後端的物件鍵值。</summary>
    /// <example>public/3fa85f64-5717-4562-b3fc-2c963f66afa6/.../avatar.png</example>
    public string StorageKey { get; set; } = "";

    /// <summary>公開讀取網址；僅 `IsPublic=true` 時提供。</summary>
    /// <example>http://localhost:5171/v1/files/blob/public/3fa85f64-5717-4562-b3fc-2c963f66afa6/.../avatar.png</example>
    public string? PublicUrl { get; set; }

    /// <summary>簽章 URL 過期時間（UTC）。</summary>
    public DateTimeOffset ExpiresAt { get; set; }
}

/// <summary>下載簽章 URL 回應。</summary>
public class GetDownloadUrlResponse
{
    /// <summary>檔案 ID。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid FileId { get; set; }

    /// <summary>有效的短效下載 URL；調用方應立即轉交給終端使用者，不應快取。</summary>
    /// <example>http://localhost:5171/v1/files/blob/creators/.../intro-video.mp4?expires=1735689600&amp;sig=...</example>
    public string DownloadUrl { get; set; } = "";

    /// <summary>簽章 URL 過期時間（UTC）。</summary>
    public DateTimeOffset ExpiresAt { get; set; }
}

/// <summary>租戶實際用量回應（每日對帳用，加總已 Ready 檔案大小）。</summary>
public class TenantUsageResponse
{
    /// <summary>租戶（創作者）ID。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid CreatorId { get; set; }

    /// <summary>該租戶已 Ready 檔案的位元組總和。</summary>
    /// <example>1048576</example>
    public long TotalBytes { get; set; }
}

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
