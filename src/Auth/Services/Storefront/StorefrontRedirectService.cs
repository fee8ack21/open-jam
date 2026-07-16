using Auth.Data;
using Auth.Options;
using Auth.Services.Hydra;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shared.Data;

namespace Auth.Services.Storefront;

/// <summary>
/// 店面子網域 OIDC redirect URI 註冊實作。
/// Hydra 更新 client 為整包 read-modify-write（GET → 改 redirect_uris → PUT），
/// 併發寫入會互相覆蓋（lost update），故以 PostgreSQL advisory lock 序列化：
/// 同一時間只有一個註冊流程能改 web client，鎖跨副本生效、交易結束自動釋放。
/// </summary>
public class StorefrontRedirectService(
    AppDbContext db,
    IHydraService hydra,
    IConfiguration config,
    IOptions<AppOptions> appOptions,
    ILogger<StorefrontRedirectService> logger) : IStorefrontRedirectService
{
    /// <summary>advisory lock 鍵值，僅用於「Hydra web client 更新」這一件事，全服務唯一即可。</summary>
    private const long HydraWebClientLockKey = 728_349_501;

    /// <inheritdoc/>
    public async Task RegisterAsync(string storeSlug, CancellationToken ct)
    {
        var clientId = config["Hydra:WebClientId"] ?? "open-jam-web";
        var pattern  = appOptions.Value.StorefrontUrlPattern;

        // EnableRetryOnFailure 下顯式交易須包進 execution strategy；委派內容冪等，重放安全。
        await db.Database.ExecuteInTransactionAsync(async tx =>
        {
            await db.Database.ExecuteSqlRawAsync(
                $"SELECT pg_advisory_xact_lock({HydraWebClientLockKey})", ct);

            var client = await hydra.GetClientAsync(clientId, ct)
                ?? throw new InvalidOperationException($"Hydra client '{clientId}' 不存在，無法註冊店面 redirect URI。");

            if (StorefrontRedirectUris.MergeInto(client, pattern, storeSlug))
            {
                await hydra.PutClientAsync(clientId, client, ct);
                logger.LogInformation("已將店面 '{Slug}' 的 redirect URI 註冊進 Hydra client '{ClientId}'。", storeSlug, clientId);
            }

            await tx.CommitAsync(ct);
        }, ct);
    }
}
