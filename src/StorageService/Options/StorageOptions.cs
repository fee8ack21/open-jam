namespace StorageService.Options;

/// <summary>物件儲存後端設定（地端 MinIO / 雲端 GCS 共用介面）。</summary>
public class StorageOptions
{
    /// <summary>MinIO / GCS 端點，例如 "localhost:9000"。</summary>
    public string Endpoint { get; set; } = "localhost:9000";

    /// <summary>Access key（MinIO root user / GCS HMAC key）。</summary>
    public string AccessKey { get; set; } = "minioadmin";

    /// <summary>Secret key（MinIO root password / GCS HMAC secret）。</summary>
    public string SecretKey { get; set; } = "minioadmin";

    /// <summary>是否使用 TLS；本地 MinIO 設 false。</summary>
    public bool UseSsl { get; set; } = false;

    /// <summary>目標 bucket 名稱。</summary>
    public string Bucket { get; set; } = "open-jam";

    /// <summary>上傳簽章 URL 有效秒數；預設 1 小時。</summary>
    public int UploadUrlExpirySeconds { get; set; } = 3600;

    /// <summary>下載簽章 URL 有效秒數；預設 5 分鐘。</summary>
    public int DownloadUrlExpirySeconds { get; set; } = 300;

    /// <summary>上傳逾時未確認的孤兒檔案判定時數；預設 24 小時。</summary>
    public int UploadTimeoutHours { get; set; } = 24;

    /// <summary>軟刪除後保留天數，到期才從儲存後端永久刪除；預設 30 天。</summary>
    public int SoftDeleteRetentionDays { get; set; } = 30;
}
