namespace Shared.Auth;

/// <summary>
/// 服務間（service-to-service）認證設定，對應 appsettings <c>ServiceAuth</c> 區段。
/// 呼叫端以 Hydra <c>client_credentials</c> 取得 service token；
/// 被呼叫端以 <c>ClientId</c> 驗證 token 的 <c>sub</c> claim（見 <c>AddOpenJamInternalServicePolicy</c>）。
/// </summary>
public class ServiceAuthOptions
{
    /// <summary>Hydra OAuth2 token endpoint（呼叫端取 token 用）。</summary>
    /// <example>http://localhost:4444/oauth2/token</example>
    public string TokenUrl { get; set; } = "";

    /// <summary>服務用 OAuth2 client id（Bootstrap <c>HydraClientSeeder</c> 所 seed 的 Service client）。</summary>
    /// <example>open-jam-service</example>
    public string ClientId { get; set; } = "open-jam-service";

    /// <summary>服務用 OAuth2 client secret（呼叫端取 token 用；正式以 Secret 注入）。</summary>
    public string ClientSecret { get; set; } = "";
}
