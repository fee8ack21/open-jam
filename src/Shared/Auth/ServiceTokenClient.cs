using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Shared.Auth;

/// <summary>
/// 以 Hydra <c>client_credentials</c> 取得服務間呼叫用 access token 的客戶端（singleton）。
/// Token 於程序內快取至到期前一分鐘，避免每次呼叫都向 Hydra 換發。
/// </summary>
public class ServiceTokenClient(IHttpClientFactory httpClientFactory, IOptions<ServiceAuthOptions> options)
{
    private static readonly TimeSpan ExpirySafetyMargin = TimeSpan.FromMinutes(1);

    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private string? _token;
    private DateTimeOffset _expiresAt;

    /// <summary>取得有效的 service access token（必要時向 Hydra 換發並快取）。</summary>
    /// <param name="ct">Cancellation token。</param>
    public async Task<string> GetTokenAsync(CancellationToken ct)
    {
        if (_token is { } cached && DateTimeOffset.UtcNow < _expiresAt)
            return cached;

        await _refreshLock.WaitAsync(ct);
        try
        {
            if (_token is { } refreshed && DateTimeOffset.UtcNow < _expiresAt)
                return refreshed;

            var opts = options.Value;
            var client = httpClientFactory.CreateClient("service-auth");

            using var request = new HttpRequestMessage(HttpMethod.Post, opts.TokenUrl)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["scope"] = "openid",
                }),
            };
            // client_secret_basic：RFC 6749 要求 id / secret 先 form-urlencode 再組 Basic 認證。
            var basic = $"{Uri.EscapeDataString(opts.ClientId)}:{Uri.EscapeDataString(opts.ClientSecret)}";
            request.Headers.Authorization = new("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(basic)));

            using var response = await client.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var token = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: ct)
                ?? throw new HttpRequestException("Hydra token endpoint 回應內容為空。");

            _token = token.AccessToken;
            _expiresAt = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn) - ExpirySafetyMargin;
            return _token;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = "";

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
