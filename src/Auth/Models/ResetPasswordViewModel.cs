using System.ComponentModel.DataAnnotations;

namespace Auth.Models;

/// <summary>重置密碼表單 ViewModel。</summary>
public class ResetPasswordViewModel
{
    /// <summary>重置連結中的 token，由 GET 動作從 query string 帶入並透過 hidden field 傳回 POST。</summary>
    /// <example>a3f9e1...</example>
    public string Token { get; set; } = "";

    /// <summary>新密碼（8–20 字，需含大小寫英文、數字及特殊符號）。</summary>
    /// <example>NewPass@456</example>
    [Required(ErrorMessage = "請設定新密碼")]
    [ValidPassword]
    public string Password { get; set; } = "";

    /// <summary>確認新密碼，需與 Password 相同。</summary>
    /// <example>NewPass@456</example>
    [Required(ErrorMessage = "請再次輸入密碼")]
    [Compare("Password", ErrorMessage = "兩次輸入的密碼不一致")]
    public string ConfirmPassword { get; set; } = "";
}
