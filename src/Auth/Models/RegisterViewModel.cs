using System.ComponentModel.DataAnnotations;

namespace Auth.Models;

/// <summary>註冊表單 ViewModel。</summary>
public class RegisterViewModel
{
    /// <summary>電子信箱。</summary>
    /// <example>user@example.com</example>
    [Required(ErrorMessage = "請輸入電子信箱")]
    [EmailAddress(ErrorMessage = "請輸入正確的電子信箱")]
    public string Email { get; set; } = "";

    /// <summary>密碼（8–20 字，需含大小寫英文、數字及特殊符號）。</summary>
    /// <example>MyPass@123</example>
    [Required(ErrorMessage = "請設定密碼")]
    [ValidPassword]
    public string Password { get; set; } = "";

    /// <summary>確認密碼，需與 Password 相同。</summary>
    /// <example>MyPass@123</example>
    [Required(ErrorMessage = "請再次輸入密碼")]
    [Compare("Password", ErrorMessage = "兩次密碼不一致")]
    public string ConfirmPassword { get; set; } = "";

    /// <summary>是否同意服務條款與隱私權政策。</summary>
    /// <example>true</example>
    [MustBeTrue(ErrorMessage = "請先同意服務條款")]
    public bool Agree { get; set; }
}
