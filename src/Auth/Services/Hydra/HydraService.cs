using System.Net.Http.Json;

namespace Auth.Services.Hydra;

public class HydraService(IHttpClientFactory factory) : IHydraService
{
    private readonly HttpClient _http = factory.CreateClient("hydra");

    public async Task<HydraLoginInfo> GetLoginInfoAsync(string challenge)
    {
        var result = await _http.GetFromJsonAsync<HydraLoginInfo>(
            $"admin/oauth2/auth/requests/login?login_challenge={Uri.EscapeDataString(challenge)}");
        return result!;
    }

    public async Task<string> AcceptLoginAsync(string challenge, HydraAcceptLoginRequest request)
    {
        var response = await _http.PutAsJsonAsync(
            $"admin/oauth2/auth/requests/login/accept?login_challenge={Uri.EscapeDataString(challenge)}",
            request);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<HydraRedirectResponse>();
        return result!.RedirectTo;
    }

    public async Task<string> RejectLoginAsync(string challenge, string error, string description)
    {
        var response = await _http.PutAsJsonAsync(
            $"admin/oauth2/auth/requests/login/reject?login_challenge={Uri.EscapeDataString(challenge)}",
            new HydraRejectRequest(error, description));
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<HydraRedirectResponse>();
        return result!.RedirectTo;
    }

    public async Task<HydraConsentInfo> GetConsentInfoAsync(string challenge)
    {
        var result = await _http.GetFromJsonAsync<HydraConsentInfo>(
            $"admin/oauth2/auth/requests/consent?consent_challenge={Uri.EscapeDataString(challenge)}");
        return result!;
    }

    public async Task<string> AcceptConsentAsync(string challenge, HydraAcceptConsentRequest request)
    {
        var response = await _http.PutAsJsonAsync(
            $"admin/oauth2/auth/requests/consent/accept?consent_challenge={Uri.EscapeDataString(challenge)}",
            request);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<HydraRedirectResponse>();
        return result!.RedirectTo;
    }

    public async Task<HydraLogoutInfo> GetLogoutInfoAsync(string challenge)
    {
        var result = await _http.GetFromJsonAsync<HydraLogoutInfo>(
            $"admin/oauth2/auth/requests/logout?logout_challenge={Uri.EscapeDataString(challenge)}");
        return result!;
    }

    public async Task<string> AcceptLogoutAsync(string challenge)
    {
        var response = await _http.PutAsync(
            $"admin/oauth2/auth/requests/logout/accept?logout_challenge={Uri.EscapeDataString(challenge)}",
            content: null);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<HydraRedirectResponse>();
        return result!.RedirectTo;
    }
}
