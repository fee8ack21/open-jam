using System.Net;
using System.Net.Http.Json;

namespace Auth.Services.Hydra;

public class HydraService(IHttpClientFactory factory) : IHydraService
{
    private readonly HttpClient _http = factory.CreateClient("hydra");

    public async Task<HydraLoginInfo> GetLoginInfoAsync(string challenge)
    {
        var response = await _http.GetAsync(
            $"admin/oauth2/auth/requests/login?login_challenge={Uri.EscapeDataString(challenge)}");
        EnsureChallengeSuccess(response);
        var result = await response.Content.ReadFromJsonAsync<HydraLoginInfo>();
        return result!;
    }

    public async Task<string> AcceptLoginAsync(string challenge, HydraAcceptLoginRequest request)
    {
        var response = await _http.PutAsJsonAsync(
            $"admin/oauth2/auth/requests/login/accept?login_challenge={Uri.EscapeDataString(challenge)}",
            request);
        EnsureChallengeSuccess(response);
        var result = await response.Content.ReadFromJsonAsync<HydraRedirectResponse>();
        return result!.RedirectTo;
    }

    public async Task<string> RejectLoginAsync(string challenge, string error, string description)
    {
        var response = await _http.PutAsJsonAsync(
            $"admin/oauth2/auth/requests/login/reject?login_challenge={Uri.EscapeDataString(challenge)}",
            new HydraRejectRequest(error, description));
        EnsureChallengeSuccess(response);
        var result = await response.Content.ReadFromJsonAsync<HydraRedirectResponse>();
        return result!.RedirectTo;
    }

    public async Task<HydraConsentInfo> GetConsentInfoAsync(string challenge)
    {
        var response = await _http.GetAsync(
            $"admin/oauth2/auth/requests/consent?consent_challenge={Uri.EscapeDataString(challenge)}");
        EnsureChallengeSuccess(response);
        var result = await response.Content.ReadFromJsonAsync<HydraConsentInfo>();
        return result!;
    }

    public async Task<string> AcceptConsentAsync(string challenge, HydraAcceptConsentRequest request)
    {
        var response = await _http.PutAsJsonAsync(
            $"admin/oauth2/auth/requests/consent/accept?consent_challenge={Uri.EscapeDataString(challenge)}",
            request);
        EnsureChallengeSuccess(response);
        var result = await response.Content.ReadFromJsonAsync<HydraRedirectResponse>();
        return result!.RedirectTo;
    }

    public async Task<HydraLogoutInfo> GetLogoutInfoAsync(string challenge)
    {
        var response = await _http.GetAsync(
            $"admin/oauth2/auth/requests/logout?logout_challenge={Uri.EscapeDataString(challenge)}");
        EnsureChallengeSuccess(response);
        var result = await response.Content.ReadFromJsonAsync<HydraLogoutInfo>();
        return result!;
    }

    public async Task<string> AcceptLogoutAsync(string challenge)
    {
        var response = await _http.PutAsync(
            $"admin/oauth2/auth/requests/logout/accept?logout_challenge={Uri.EscapeDataString(challenge)}",
            content: null);
        EnsureChallengeSuccess(response);
        var result = await response.Content.ReadFromJsonAsync<HydraRedirectResponse>();
        return result!.RedirectTo;
    }

    /// <summary>
    /// challenge 無效（格式錯誤 400、不存在 404、已被使用 410）時 Hydra 回對應狀態碼，
    /// 統一轉為 InvalidChallengeException；其餘非 2xx 視為基礎設施異常照常拋出。
    /// </summary>
    private static void EnsureChallengeSuccess(HttpResponseMessage response)
    {
        if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.NotFound or HttpStatusCode.Gone)
            throw new InvalidChallengeException();
        response.EnsureSuccessStatusCode();
    }
}
