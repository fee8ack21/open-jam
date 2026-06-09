using System.ComponentModel.DataAnnotations;

namespace Auth.Models;

/// <summary>忘記密碼表單 ViewModel。</summary>
public class ForgotPasswordViewModel
{
    /// <summary>電子信箱。</summary>
    /// <example>user@example.com</example>
    [Required(ErrorMessage = "請輸入電子信箱")]
    [EmailAddress(ErrorMessage = "請輸入正確的電子信箱")]
    public string Email { get; set; } = "";

    /// <summary>Hydra 登入挑戰參數，從 Login 頁帶入以供返回時還原 OIDC flow。</summary>
    /// <example>abc123challenge</example>
    public string? LoginChallenge { get; set; }
}
