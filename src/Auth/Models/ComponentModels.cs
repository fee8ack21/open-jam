using Microsoft.AspNetCore.Html;

namespace Auth.Models;

/// <summary>通用輸入欄位 Partial 的 model。</summary>
public record FieldModel
{
    /// <summary>欄位的 HTML id 與 name。</summary>
    /// <example>email</example>
    public required string Id { get; init; }

    /// <summary>欄位標籤文字。</summary>
    /// <example>電子信箱</example>
    public required string Label { get; init; }

    /// <summary>input type；預設為 text。</summary>
    /// <example>email</example>
    public string? Type { get; init; }

    /// <summary>欄位左側圖示的 SVG id。</summary>
    /// <example>icon-mail</example>
    public string? Icon { get; init; }

    /// <summary>欄位初始值。</summary>
    /// <example>user@example.com</example>
    public string? Value { get; init; }

    /// <summary>placeholder 提示文字。</summary>
    /// <example>請輸入電子信箱</example>
    public string? Placeholder { get; init; }

    /// <summary>驗證錯誤訊息；null 或空字串表示無錯誤。</summary>
    /// <example>請輸入電子信箱</example>
    public string? Error { get; init; }

    /// <summary>autocomplete 屬性值。</summary>
    /// <example>email</example>
    public string? AutoComplete { get; init; }

    /// <summary>標籤右側額外 HTML 內容（如「忘記密碼？」連結）。</summary>
    public IHtmlContent? LabelRight { get; init; }
}

/// <summary>密碼輸入欄位 Partial 的 model。</summary>
public record PasswordFieldModel
{
    /// <summary>欄位的 HTML id 與 name。</summary>
    /// <example>password</example>
    public required string Id { get; init; }

    /// <summary>欄位標籤文字。</summary>
    /// <example>密碼</example>
    public required string Label { get; init; }

    /// <summary>欄位初始值。</summary>
    /// <example>MyPass@123</example>
    public string? Value { get; init; }

    /// <summary>placeholder 提示文字。</summary>
    /// <example>請輸入密碼</example>
    public string? Placeholder { get; init; }

    /// <summary>驗證錯誤訊息；null 或空字串表示無錯誤。</summary>
    /// <example>請設定密碼</example>
    public string? Error { get; init; }

    /// <summary>是否顯示密碼強度 meter。</summary>
    /// <example>true</example>
    public bool ShowStrength { get; init; }

    /// <summary>autocomplete 屬性值。</summary>
    /// <example>current-password</example>
    public string? AutoComplete { get; init; }

    /// <summary>標籤右側額外 HTML 內容。</summary>
    public IHtmlContent? LabelRight { get; init; }

    /// <summary>是否預設顯示明文（visibility toggle 初始狀態）。</summary>
    /// <example>false</example>
    public bool Show { get; init; }
}

/// <summary>Checkbox 欄位 Partial 的 model。</summary>
public record CheckboxModel
{
    /// <summary>欄位的 HTML id 與 name。</summary>
    /// <example>agree</example>
    public required string Id { get; init; }

    /// <summary>Checkbox 旁的標籤文字（可含 HTML）。</summary>
    /// <example>我同意服務條款</example>
    public required string Label { get; init; }

    /// <summary>Checkbox 初始勾選狀態。</summary>
    /// <example>false</example>
    public bool Checked { get; init; }
}

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
