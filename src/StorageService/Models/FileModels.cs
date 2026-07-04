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

/// <summary>租戶實際用量回應（每日對帳用，加總已 Ready 且已被使用（referenced）的檔案大小）。</summary>
public class TenantUsageResponse
{
    /// <summary>租戶（創作者）ID。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid CreatorId { get; set; }

    /// <summary>該租戶已 Ready 且已被使用檔案的位元組總和。</summary>
    /// <example>1048576</example>
    public long TotalBytes { get; set; }
}

/// <summary>單一創作者用量明細。</summary>
public class CreatorUsageDto
{
    /// <summary>創作者 ID。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid CreatorId { get; set; }

    /// <summary>已 Ready 檔案數。</summary>
    /// <example>312</example>
    public int FileCount { get; set; }

    /// <summary>已 Ready 檔案位元組總和。</summary>
    /// <example>19783458816</example>
    public long Bytes { get; set; }
}

/// <summary>
/// 平台儲存用量彙總（Admin）。僅涵蓋 StorageService 可實際統計的指標：
/// 物件數量 / 大小（依公開展示 vs 私有可下載拆分）與孤兒檔。
/// 下載流量與歷史趨勢需另建指標管線，不在此端點。
/// </summary>
public class PlatformUsageResponse
{
    /// <summary>已 Ready 檔案位元組總和（公開 + 私有）。</summary>
    /// <example>71403606016</example>
    public long UsedBytes { get; set; }

    /// <summary>已 Ready 檔案總數。</summary>
    /// <example>1180</example>
    public int FileCount { get; set; }

    /// <summary>公開展示型資產（預覽 / 縮圖）位元組總和。</summary>
    /// <example>21260124160</example>
    public long PublicBytes { get; set; }

    /// <summary>私有可下載資產位元組總和。</summary>
    /// <example>50143481856</example>
    public long PrivateBytes { get; set; }

    /// <summary>孤兒檔數量（上傳未完成 / 處理失敗，保留期過後將清理）。</summary>
    /// <example>23</example>
    public int OrphanFileCount { get; set; }

    /// <summary>孤兒檔位元組總和（含未確認大小者以 0 計）。</summary>
    /// <example>104857600</example>
    public long OrphanBytes { get; set; }

    /// <summary>用量前幾名的創作者明細（依位元組數遞減）。</summary>
    public List<CreatorUsageDto> ByCreator { get; set; } = [];
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

    /// <summary>在儲存後端的物件鍵值。</summary>
    /// <example>creators/3fa85f64-5717-4562-b3fc-2c963f66afa6/.../intro-video.mp4</example>
    public string StorageKey { get; set; } = "";

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

    /// <summary>功能 API 確認此檔已被實際使用的時間；null 表示尚未被使用。</summary>
    public DateTimeOffset? ReferencedAt { get; set; }

    /// <summary>建立時間（UTC）。</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>最後更新時間（UTC）。</summary>
    public DateTimeOffset? UpdatedAt { get; set; }
}
