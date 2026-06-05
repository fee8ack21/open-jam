using System.Text.Json.Serialization;

namespace Auth.Services.Hydra;

/// <summary>Hydra 登入挑戰資訊。</summary>
/// <param name="Skip">true 表示 Hydra 已有 session，可直接接受，不需再次驗證。</param>
/// <param name="Subject">當前 session 的使用者識別碼。</param>
public record HydraLoginInfo(
    [property: JsonPropertyName("skip")]    bool   Skip,
    [property: JsonPropertyName("subject")] string Subject
);

/// <summary>向 Hydra 接受登入挑戰時送出的請求 body。</summary>
/// <param name="Subject">已驗證的使用者識別碼。</param>
/// <param name="Remember">是否請 Hydra 記住此 session。</param>
/// <param name="RememberFor">session 記憶時效（秒）；0 表示僅本次瀏覽器會話。</param>
public record HydraAcceptLoginRequest(
    [property: JsonPropertyName("subject")]      string Subject,
    [property: JsonPropertyName("remember")]     bool   Remember,
    [property: JsonPropertyName("remember_for")] int    RememberFor
);

/// <summary>Hydra 同意（consent）挑戰資訊。</summary>
/// <param name="Skip">true 表示使用者已授權過相同 scope，可直接接受。</param>
/// <param name="Subject">正在授權的使用者識別碼。</param>
/// <param name="RequestedScope">應用程式請求的 OAuth scope 清單。</param>
/// <param name="RequestedAudience">應用程式請求的 access token audience 清單。</param>
public record HydraConsentInfo(
    [property: JsonPropertyName("skip")]                           bool     Skip,
    [property: JsonPropertyName("subject")]                        string   Subject,
    [property: JsonPropertyName("requested_scope")]                string[] RequestedScope,
    [property: JsonPropertyName("requested_access_token_audience")] string[] RequestedAudience
);

/// <summary>向 Hydra 接受同意挑戰時送出的請求 body。</summary>
/// <param name="GrantScope">實際授予的 OAuth scope 清單。</param>
/// <param name="GrantAudience">實際授予的 access token audience 清單。</param>
/// <param name="Remember">是否記住此授權，下次跳過同意頁。</param>
/// <param name="RememberFor">授權記憶時效（秒）。</param>
/// <param name="Session">注入 token 的額外 claims，如 id_token 自訂欄位。</param>
public record HydraAcceptConsentRequest(
    [property: JsonPropertyName("grant_scope")]                    string[]            GrantScope,
    [property: JsonPropertyName("grant_access_token_audience")]    string[]            GrantAudience,
    [property: JsonPropertyName("remember")]                       bool                Remember,
    [property: JsonPropertyName("remember_for")]                   int                 RememberFor,
    [property: JsonPropertyName("session")]                        HydraConsentSession Session
);

/// <summary>同意 session 中可附加至 token 的自訂 claims。</summary>
/// <param name="IdToken">要寫入 id_token 的額外 claims；null 表示不附加。</param>
public record HydraConsentSession(
    [property: JsonPropertyName("id_token")] Dictionary<string, object>? IdToken
);

/// <summary>向 Hydra 拒絕登入或同意挑戰時送出的請求 body。</summary>
/// <param name="Error">OAuth 標準錯誤碼，如 access_denied。</param>
/// <param name="ErrorDescription">人類可讀的錯誤說明。</param>
public record HydraRejectRequest(
    [property: JsonPropertyName("error")]             string Error,
    [property: JsonPropertyName("error_description")] string ErrorDescription
);

/// <summary>Hydra 登出挑戰資訊。</summary>
/// <param name="Subject">正在登出的使用者識別碼。</param>
/// <param name="Sid">正在登出的 session ID。</param>
public record HydraLogoutInfo(
    [property: JsonPropertyName("subject")] string Subject,
    [property: JsonPropertyName("sid")]     string Sid
);

/// <summary>Hydra 在接受 login / consent / logout 後回傳的重導向資訊。</summary>
/// <param name="RedirectTo">應用程式須重導向至的 URL。</param>
public record HydraRedirectResponse(
    [property: JsonPropertyName("redirect_to")] string RedirectTo
);
