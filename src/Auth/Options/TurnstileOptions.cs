namespace Auth.Options;

/// <summary>Cloudflare Turnstile 人機驗證設定（認證表單防機器人）。</summary>
public class TurnstileOptions
{
    /// <summary>widget Site Key（公開，渲染於表單頁面）。</summary>
    /// <example>1x00000000000000000000AA</example>
    public string SiteKey { get; set; } = "";

    /// <summary>siteverify Secret Key（機密，正式環境以 Secret 注入）。</summary>
    /// <example>1x0000000000000000000000000000000AA</example>
    public string SecretKey { get; set; } = "";

    /// <summary>兩把金鑰皆有值才啟用；清空任一即整組停用（widget 不渲染、後端不驗），作為 kill switch。</summary>
    public bool Enabled => !string.IsNullOrEmpty(SiteKey) && !string.IsNullOrEmpty(SecretKey);
}
