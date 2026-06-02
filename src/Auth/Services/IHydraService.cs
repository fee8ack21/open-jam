using Auth.Models;

namespace Auth.Services;

public interface IHydraService
{
    Task<HydraLoginInfo>   GetLoginInfoAsync(string challenge);
    Task<string>           AcceptLoginAsync(string challenge, HydraAcceptLoginRequest request);
    Task<string>           RejectLoginAsync(string challenge, string error, string description);
    Task<HydraConsentInfo> GetConsentInfoAsync(string challenge);
    Task<string>           AcceptConsentAsync(string challenge, HydraAcceptConsentRequest request);
    Task<HydraLogoutInfo>  GetLogoutInfoAsync(string challenge);
    Task<string>           AcceptLogoutAsync(string challenge);
}
