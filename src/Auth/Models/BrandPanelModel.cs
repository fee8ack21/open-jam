namespace Auth.Models;

/// <summary>品牌側欄 Partial 的 model。</summary>
public record BrandPanelModel
{
    /// <summary>主標題文字。</summary>
    /// <example>歡迎回來</example>
    public required string Headline { get; init; }

    /// <summary>副標題文字。</summary>
    /// <example>登入以繼續管理您的商店</example>
    public required string Sub { get; init; }
}
