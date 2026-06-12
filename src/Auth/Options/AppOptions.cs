namespace Auth.Options;

/// <summary>應用程式基本設定。</summary>
public class AppOptions
{
    /// <summary>應用程式對外根 URL，供產生絕對連結使用。</summary>
    /// <example>https://auth.openjam.co</example>
    public string BaseUrl { get; set; } = null!;

    /// <summary>Workspace Web 根 URL，無 OIDC challenge 時導向此處發起 OIDC flow。</summary>
    /// <example>https://workspace.openjam.co</example>
    public string WorkspaceUrl { get; set; } = null!;
}
