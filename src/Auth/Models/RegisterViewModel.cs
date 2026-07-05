using System.ComponentModel.DataAnnotations;
using Auth.Common.Validators;

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

    /// <summary>Hydra 登入挑戰參數，從 Login 頁帶入以供返回時還原 OIDC flow。</summary>
    /// <example>abc123challenge</example>
    public string? LoginChallenge { get; set; }

    /// <summary>目前啟用中的服務條款（僅供畫面渲染，POST 時由伺服器重查，不從表單回傳）。</summary>
    public LegalDocumentDto? TermsDocument { get; set; }

    /// <summary>目前啟用中的隱私權政策（僅供畫面渲染，POST 時由伺服器重查，不從表單回傳）。</summary>
    public LegalDocumentDto? PrivacyDocument { get; set; }
}
