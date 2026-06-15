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

    /// <inheritdoc/>
    public Task<string> GenerateUploadUrlAsync(string key, string contentType, long maxBytes, TimeSpan expiry, CancellationToken ct = default) =>
        Task.FromResult(BuildSignedUrl("PUT", key, expiry));

    /// <inheritdoc/>
    public Task<string> GenerateDownloadUrlAsync(string key, TimeSpan expiry, CancellationToken ct = default) =>
        Task.FromResult(BuildSignedUrl("GET", key, expiry));

    /// <inheritdoc/>
    public Task DeleteAsync(string key, CancellationToken ct = default)
    {
        fileStore.Delete(key);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<bool> ObjectExistsAsync(string key, CancellationToken ct = default) =>
        Task.FromResult(fileStore.Exists(key));

    /// <inheritdoc/>
    public Task EnsurePublicReadPolicyAsync(CancellationToken ct = default)
    {
        // 本地儲存無 bucket policy 概念；確保根目錄存在即可。
        // 公開讀取（public/* 前綴）由 blob 下載端點免簽章放行。
        fileStore.EnsureRoot();
        return Task.CompletedTask;
    }

    /// <summary>組合指向本服務 blob 端點、帶 HMAC 簽章與到期時間的 URL。</summary>
    private string BuildSignedUrl(string method, string key, TimeSpan expiry)
    {
        var expiresUnix = DateTimeOffset.UtcNow.Add(expiry).ToUnixTimeSeconds();
        var sig = signer.Sign(method, key, expiresUnix);
        var baseUrl = _local.BaseUrl.TrimEnd('/');

        // 逐段 URL 編碼但保留路徑分隔符，避免檔名含空白 / 特殊字元時 URL 失效。
        var encodedKey = string.Join('/', key.Split('/').Select(Uri.EscapeDataString));

        return $"{baseUrl}/v1/files/blob/{encodedKey}?expires={expiresUnix}&sig={sig}";
    }
}
