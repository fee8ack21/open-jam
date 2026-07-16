using Microsoft.Extensions.Options;
using StorageService.Options;

namespace StorageService.Storage;

/// <summary>
/// 以地端本地檔案系統實作的儲存後端，用於本地開發環境。
/// 檔案存放於設定的根目錄（預設 <c>Files</c>），上傳 / 下載 URL 指回本服務的 blob 端點，
/// 並以 <see cref="BlobUrlSigner"/> 的 HMAC 簽章扮演雲端 presigned URL 在地端的對應角色。
/// </summary>
public class LocalStorageProvider(
    LocalFileStore fileStore,
    BlobUrlSigner signer,
    IOptions<StorageOptions> options) : IStorageProvider
{
    private readonly LocalOptions _local = options.Value.Local;

    /// <summary>
    /// 將物件鍵值映射為 blob 端點路徑：公開物件加上 <c>public/</c> 虛擬 bucket 路徑段
    /// （對應雲端的公開 bucket，blob 下載端點以此段免簽章放行），私有物件直接使用鍵值。
    /// </summary>
    private static string BlobPath(string key, bool isPublic) => isPublic ? $"public/{key}" : key;

    /// <inheritdoc/>
    public Task<string> GenerateUploadUrlAsync(string key, bool isPublic, string contentType, long maxBytes, TimeSpan expiry, CancellationToken ct = default) =>
        Task.FromResult(BuildSignedUrl("PUT", BlobPath(key, isPublic), expiry, maxBytes));

    /// <inheritdoc/>
    public Task<string> GenerateDownloadUrlAsync(string key, bool isPublic, TimeSpan expiry, CancellationToken ct = default) =>
        Task.FromResult(BuildSignedUrl("GET", BlobPath(key, isPublic), expiry));

    /// <inheritdoc/>
    public Task DeleteAsync(string key, bool isPublic, CancellationToken ct = default)
    {
        fileStore.Delete(BlobPath(key, isPublic));
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<bool> ObjectExistsAsync(string key, bool isPublic, CancellationToken ct = default) =>
        Task.FromResult(fileStore.Exists(BlobPath(key, isPublic)));

    /// <inheritdoc/>
    public Task EnsurePublicReadPolicyAsync(CancellationToken ct = default)
    {
        // 本地儲存無 bucket policy 概念；確保根目錄存在即可。
        // 公開讀取（public/* 虛擬 bucket 路徑段）由 blob 下載端點免簽章放行。
        fileStore.EnsureRoot();
        return Task.CompletedTask;
    }

    /// <summary>組合指向本服務 blob 端點、帶 HMAC 簽章與到期時間的 URL。上傳（PUT）另綁大小上限。</summary>
    private string BuildSignedUrl(string method, string blobPath, TimeSpan expiry, long maxBytes = 0)
    {
        var expiresUnix = DateTimeOffset.UtcNow.Add(expiry).ToUnixTimeSeconds();
        var sig = signer.Sign(method, blobPath, expiresUnix, maxBytes);
        var baseUrl = _local.BaseUrl.TrimEnd('/');

        // 逐段 URL 編碼但保留路徑分隔符，避免檔名含空白 / 特殊字元時 URL 失效。
        var encodedPath = string.Join('/', blobPath.Split('/').Select(Uri.EscapeDataString));

        var url = $"{baseUrl}/v1/files/blob/{encodedPath}?expires={expiresUnix}&sig={sig}";
        // PUT 才帶大小上限；blob 端點驗章時一併驗 max，並於接收時強制。
        return maxBytes > 0 ? $"{url}&max={maxBytes}" : url;
    }
}
