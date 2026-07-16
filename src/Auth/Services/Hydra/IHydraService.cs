using System.Text.Json.Nodes;

namespace Auth.Services.Hydra;

/// <summary>封裝 Ory Hydra Admin API 的 login / consent / logout 流程與 OAuth2 client 維護。</summary>
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

    /// <summary>
    /// 取得 OAuth2 client 完整設定；不存在回傳 null。
    /// 以 <see cref="JsonNode"/> 承載避免有損的型別對應（改動後可原樣 PUT 回，保留未建模欄位）。
    /// </summary>
    Task<JsonNode?>        GetClientAsync(string clientId, CancellationToken ct = default);

    /// <summary>整包更新 OAuth2 client（PUT），<paramref name="client"/> 需為 GetClientAsync 取回後修改的完整物件。</summary>
    Task                   PutClientAsync(string clientId, JsonNode client, CancellationToken ct = default);
}
