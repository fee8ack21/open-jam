using System.Text.Json.Serialization;
using Auth.Options;
using Microsoft.Extensions.Options;

namespace Auth.Services.Security;

/// <summary>以 Cloudflare siteverify API 驗證 Turnstile token。</summary>
public class TurnstileService(
    IHttpClientFactory httpClientFactory,
    IOptions<TurnstileOptions> options,
    ILogger<TurnstileService> logger) : ITurnstileService
{
    private sealed record SiteverifyResponse(
        bool Success,
        [property: JsonPropertyName("error-codes")] string[]? ErrorCodes);

    public async Task<bool> VerifyAsync(string? token, string? remoteIp)
    {
        if (!options.Value.Enabled)
            return true;

        if (string.IsNullOrEmpty(token))
            return false;

        try
        {
            var client = httpClientFactory.CreateClient("turnstile");
            using var response = await client.PostAsync("turnstile/v0/siteverify",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["secret"]   = options.Value.SecretKey,
                    ["response"] = token,
                    ["remoteip"] = remoteIp ?? "",
                }));
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<SiteverifyResponse>();
            if (result is { Success: true })
                return true;

            logger.LogInformation("Turnstile 驗證未通過：{ErrorCodes}",
                string.Join(",", result?.ErrorCodes ?? []));
            return false;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Turnstile siteverify 呼叫失敗，fail-open 放行本次請求");
            return true;
        }
    }
}
