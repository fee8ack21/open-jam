namespace StorageService.Options;

/// <summary>物件儲存後端設定（地端 MinIO / 雲端 GCS 共用介面）。</summary>
public class StorageOptions
{
    /// <summary>儲存後端供應商：<c>Minio</c>（地端開發，預設）或 <c>Gcs</c>（雲端正式）。</summary>
    public StorageProvider Provider { get; set; } = StorageProvider.Minio;

    /// <summary>Google Cloud Storage 專屬設定；僅 <see cref="Provider"/> 為 <c>Gcs</c> 時使用。</summary>
    public GcsOptions Gcs { get; set; } = new();

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

    /// <summary>公開讀取物件（`public/*`）的對外存取網址前綴，例如 "http://localhost:9000/open-jam"。</summary>
    public string PublicBaseUrl { get; set; } = "";

    /// <summary>上傳簽章 URL 有效秒數；預設 1 小時。</summary>
    public int UploadUrlExpirySeconds { get; set; } = 3600;

    /// <summary>下載簽章 URL 有效秒數；預設 5 分鐘。</summary>
    public int DownloadUrlExpirySeconds { get; set; } = 300;

    /// <summary>上傳逾時未確認的孤兒檔案判定時數；預設 24 小時。</summary>
    public int UploadTimeoutHours { get; set; } = 24;

    /// <summary>軟刪除後保留天數，到期才從儲存後端永久刪除；預設 30 天。</summary>
    public int SoftDeleteRetentionDays { get; set; } = 30;
}

/// <summary>儲存後端供應商種類。</summary>
public enum StorageProvider
{
    /// <summary>地端 MinIO（S3 相容），用於本地開發。</summary>
    Minio,

    /// <summary>Google Cloud Storage，用於雲端正式環境。</summary>
    Gcs,
}

/// <summary>Google Cloud Storage 專屬設定。</summary>
public class GcsOptions
{
    /// <summary>
    /// 服務帳戶金鑰 JSON 檔路徑；簽發 V4 signed URL 需要可簽章的憑證。
    /// 留空時改用 Application Default Credentials（GKE Workload Identity），
    /// 此時 signed URL 透過 IAM SignBlob API 簽章，服務帳戶需具備 <c>iam.serviceAccounts.signBlob</c> 權限。
    /// </summary>
    public string? CredentialsPath { get; set; }
}
