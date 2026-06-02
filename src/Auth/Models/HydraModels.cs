using System.Text.Json.Serialization;

namespace Auth.Models;

public record HydraLoginInfo(
    [property: JsonPropertyName("skip")]    bool   Skip,
    [property: JsonPropertyName("subject")] string Subject
);

public record HydraAcceptLoginRequest(
    [property: JsonPropertyName("subject")]      string Subject,
    [property: JsonPropertyName("remember")]     bool   Remember,
    [property: JsonPropertyName("remember_for")] int    RememberFor
);

public record HydraConsentInfo(
    [property: JsonPropertyName("skip")]                           bool     Skip,
    [property: JsonPropertyName("subject")]                        string   Subject,
    [property: JsonPropertyName("requested_scope")]                string[] RequestedScope,
    [property: JsonPropertyName("requested_access_token_audience")] string[] RequestedAudience
);

public record HydraAcceptConsentRequest(
    [property: JsonPropertyName("grant_scope")]                    string[]            GrantScope,
    [property: JsonPropertyName("grant_access_token_audience")]    string[]            GrantAudience,
    [property: JsonPropertyName("remember")]                       bool                Remember,
    [property: JsonPropertyName("remember_for")]                   int                 RememberFor,
    [property: JsonPropertyName("session")]                        HydraConsentSession Session
);

public record HydraConsentSession(
    [property: JsonPropertyName("id_token")] Dictionary<string, object>? IdToken
);

public record HydraRejectRequest(
    [property: JsonPropertyName("error")]             string Error,
    [property: JsonPropertyName("error_description")] string ErrorDescription
);

public record HydraLogoutInfo(
    [property: JsonPropertyName("subject")] string Subject,
    [property: JsonPropertyName("sid")]     string Sid
);

public record HydraRedirectResponse(
    [property: JsonPropertyName("redirect_to")] string RedirectTo
);
