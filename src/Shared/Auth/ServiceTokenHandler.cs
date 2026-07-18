using System.Net.Http.Headers;

namespace Shared.Auth;

/// <summary>
/// 為 named HttpClient 自動注入服務間呼叫用 Bearer token 的 <see cref="DelegatingHandler"/>。
/// 於呼叫端以 <c>AddHttpMessageHandler&lt;ServiceTokenHandler&gt;()</c> 掛在需認證的 client 上，
/// 使該 client 的每個請求都自動帶上 <see cref="ServiceTokenClient"/> 取得（並快取）的 token，
/// 呼叫被 <c>"InternalService"</c> policy 保護的內部端點時免逐一手動附加 header。
/// </summary>
public sealed class ServiceTokenHandler(ServiceTokenClient tokenClient) : DelegatingHandler
{
    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", await tokenClient.GetTokenAsync(cancellationToken));
        return await base.SendAsync(request, cancellationToken);
    }
}
