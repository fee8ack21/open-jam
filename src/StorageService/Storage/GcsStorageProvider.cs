using System.Net;
using Google;
using Google.Apis.Storage.v1.Data;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Options;
using StorageService.Options;

namespace StorageService.Storage;

/// <summary>以 Google Cloud Storage 實作的儲存後端，用於雲端正式環境。</summary>
public class GcsStorageProvider(
    StorageClient storage,
    UrlSigner signer,
    IOptions<StorageOptions> options,
    ILogger<GcsStorageProvider> logger) : IStorageProvider
{
    private readonly StorageOptions _opts = options.Value;

    /// <summary>依公開 / 私有旗標選擇目標 bucket。</summary>
    private string BucketFor(bool isPublic) => isPublic ? _opts.PublicBucket : _opts.PrivateBucket;

    /// <inheritdoc/>
    public async Task<string> GenerateUploadUrlAsync(string key, bool isPublic, string contentType, long maxBytes, TimeSpan expiry, CancellationToken ct = default)
    {
        // 簽入 Content-Type；上傳端送出的 Content-Type header 須與此一致，否則簽章驗證失敗。
        // maxBytes 已於簽發入口（FileService）以宣稱大小擋下超過上限者；simple signed PUT URL
        // 無法在 GCS 端硬性強制實際 content-length（需 POST policy 的 content-length-range），
        // 殘餘超量由 confirm 階段的配額檢查（超單檔上限回 422、不建 reference）與孤兒清理兜底。
        var template = UrlSigner.RequestTemplate
            .FromBucket(BucketFor(isPublic))
            .WithObjectName(key)
            .WithHttpMethod(HttpMethod.Put)
            .WithContentHeaders(new Dictionary<string, IEnumerable<string>>
            {
                ["Content-Type"] = [contentType],
            });

        return await signer.SignAsync(template, UrlSigner.Options.FromDuration(expiry), ct);
    }

    /// <inheritdoc/>
    public async Task<string> GenerateDownloadUrlAsync(string key, bool isPublic, TimeSpan expiry, CancellationToken ct = default)
    {
        var template = UrlSigner.RequestTemplate
            .FromBucket(BucketFor(isPublic))
            .WithObjectName(key)
            .WithHttpMethod(HttpMethod.Get);

        return await signer.SignAsync(template, UrlSigner.Options.FromDuration(expiry), ct);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(string key, bool isPublic, CancellationToken ct = default)
    {
        try
        {
            await storage.DeleteObjectAsync(BucketFor(isPublic), key, cancellationToken: ct);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            // 刪除視為冪等：物件已不存在即達成目的（孤兒清理可能重複觸發）。
            logger.LogDebug("DeleteAsync: object {Key} already absent", key);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ObjectExistsAsync(string key, bool isPublic, CancellationToken ct = default)
    {
        try
        {
            await storage.GetObjectAsync(BucketFor(isPublic), key, cancellationToken: ct);
            return true;
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task EnsurePublicReadPolicyAsync(CancellationToken ct = default)
    {
        // 公開 bucket 只存放 IsPublic 物件（路由依 StoredFile.IsPublic，私有物件一律進私有 bucket），
        // 故對整個公開 bucket 無條件開放匿名讀取即可。
        // 注意：GCP IAM 禁止對 allUsers/allAuthenticatedUsers 使用條件式繫結
        //（"Conditions are not allowed on public resources"），因此不可加 Condition。
        var bucket = _opts.PublicBucket;
        const string role = "roles/storage.objectViewer";

        var policy = await storage.GetBucketIamPolicyAsync(bucket, cancellationToken: ct);
        policy.Bindings ??= [];

        var alreadyBound = policy.Bindings.Any(b =>
            b.Role == role &&
            b.Condition == null &&
            b.Members?.Contains("allUsers") == true);

        if (alreadyBound)
            return;

        policy.Bindings.Add(new Policy.BindingsData
        {
            Role = role,
            Members = ["allUsers"],
        });

        await storage.SetBucketIamPolicyAsync(bucket, policy, cancellationToken: ct);
    }
}
