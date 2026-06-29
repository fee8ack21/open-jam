namespace StorageService.Options;

/// <summary>物件儲存後端設定（地端本地檔案 / 雲端 GCS 共用介面）。</summary>
public class StorageOptions
{
    /// <summary>儲存後端供應商：<c>Local</c>（地端開發，預設）或 <c>Gcs</c>（雲端正式）。</summary>
    public StorageProvider Provider { get; set; } = StorageProvider.Local;

    /// <summary>地端本地檔案儲存設定；僅 <see cref="Provider"/> 為 <c>Local</c> 時使用。</summary>
    public LocalOptions Local { get; set; } = new();

    /// <summary>Google Cloud Storage 專屬設定；僅 <see cref="Provider"/> 為 <c>Gcs</c> 時使用。</summary>
    public GcsOptions Gcs { get; set; } = new();

    /// <summary>公開讀取資產（`public/*`，如商店 Avatar/Banner、商品縮圖）的 GCS bucket。</summary>
    public string PublicBucket { get; set; } = "";

    /// <summary>私有資產（買家授權下載的版本檔）的 GCS bucket。</summary>
    public string PrivateBucket { get; set; } = "";

    /// <summary>公開讀取物件（`public/*`）的對外存取網址前綴，例如 "http://localhost:5171/v1/files/blob"。</summary>
    public string PublicBaseUrl { get; set; } = "";

    /// <summary>上傳簽章 URL 有效秒數；預設 1 小時。</summary>
    public int UploadUrlExpirySeconds { get; set; } = 3600;

    /// <summary>下載簽章 URL 有效秒數；預設 5 分鐘。</summary>
    public int DownloadUrlExpirySeconds { get; set; } = 300;

    /// <summary>上傳逾時未確認的孤兒檔案判定時數；預設 24 小時。</summary>
    public int UploadTimeoutHours { get; set; } = 24;

    /// <summary>軟刪除後保留天數，到期才從儲存後端永久刪除；預設 30 天。</summary>
    public int SoftDeleteRetentionDays { get; set; } = 30;

    /// <summary>依物件鍵值前綴（`public/`）判定其所屬 bucket。</summary>
    /// <param name="key">物件鍵值，例如 "public/{creatorId}/{fileId}/avatar.png"。</param>
    public string BucketFor(string key) =>
        key.StartsWith("public/", StringComparison.Ordinal) ? PublicBucket : PrivateBucket;
}

/// <summary>儲存後端供應商種類。</summary>
public enum StorageProvider
{
    /// <summary>地端本地檔案系統，用於本地開發。</summary>
    Local,

    /// <summary>Google Cloud Storage，用於雲端正式環境。</summary>
    Gcs,
}

/// <summary>地端本地檔案儲存設定。</summary>
public class LocalOptions
{
    /// <summary>檔案存放根目錄；相對路徑以服務工作目錄為基準。預設 "Files"。</summary>
    public string RootPath { get; set; } = "Files";

    /// <summary>本服務對外可達的基底網址，用於組合上傳 / 下載 blob URL，例如 "http://localhost:5171"。</summary>
    public string BaseUrl { get; set; } = "http://localhost:5171";

    /// <summary>簽發 blob URL 的 HMAC 密鑰；正式環境須以環境變數 / Secret 覆蓋。</summary>
    public string SigningKey { get; set; } = "dev-local-storage-signing-key-change-me";
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
