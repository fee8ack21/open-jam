using System.ComponentModel.DataAnnotations;

namespace Auth.Models;

/// <summary>登入表單 ViewModel。</summary>
public class LoginViewModel
{
    /// <summary>電子信箱。</summary>
    /// <example>user@example.com</example>
    [Required(ErrorMessage = "請輸入電子信箱")]
    [EmailAddress(ErrorMessage = "請輸入正確的電子信箱")]
    public string Email { get; set; } = "";

    /// <summary>密碼。</summary>
    /// <example>MyPass@123</example>
    [Required(ErrorMessage = "請輸入密碼")]
    public string Password { get; set; } = "";

    /// <summary>是否記住登入狀態。</summary>
    /// <example>true</example>
    public bool Remember { get; set; } = true;

    /// <summary>Hydra 登入挑戰參數（OIDC flow 時由 Hydra 帶入）。</summary>
    /// <example>abc123challenge</example>
    public string? Challenge { get; set; }
}
