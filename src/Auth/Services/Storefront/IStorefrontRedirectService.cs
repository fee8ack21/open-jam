namespace Auth.Services.Storefront;

/// <summary>將店面子網域的 OIDC redirect URI 註冊進 Hydra web client。</summary>
public interface IStorefrontRedirectService
{
    /// <summary>
    /// 註冊指定店面 slug 的 callback / silent-renew / post-logout URI（缺漏才追加，冪等）。
    /// </summary>
    Task RegisterAsync(string storeSlug, CancellationToken ct);
}
