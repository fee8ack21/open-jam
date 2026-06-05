namespace Auth.Services.Hydra;

/// <summary>封裝 Ory Hydra Admin API 的 login / consent / logout 流程。</summary>
public interface IHydraService
{
    /// <summary>取得登入挑戰資訊，判斷是否可直接接受（skip）。</summary>
    Task<HydraLoginInfo>   GetLoginInfoAsync(string challenge);

    /// <summary>接受登入挑戰，回傳 Hydra 指定的重導向 URL。</summary>
    Task<string>           AcceptLoginAsync(string challenge, HydraAcceptLoginRequest request);

    /// <summary>拒絕登入挑戰，回傳 Hydra 指定的重導向 URL。</summary>
    Task<string>           RejectLoginAsync(string challenge, string error, string description);

    /// <summary>取得同意挑戰資訊，判斷是否可直接接受（skip）。</summary>
    Task<HydraConsentInfo> GetConsentInfoAsync(string challenge);

    /// <summary>接受同意挑戰，回傳 Hydra 指定的重導向 URL。</summary>
    Task<string>           AcceptConsentAsync(string challenge, HydraAcceptConsentRequest request);

    /// <summary>取得登出挑戰資訊。</summary>
    Task<HydraLogoutInfo>  GetLogoutInfoAsync(string challenge);

    /// <summary>接受登出挑戰，回傳 Hydra 指定的重導向 URL。</summary>
    Task<string>           AcceptLogoutAsync(string challenge);
}
