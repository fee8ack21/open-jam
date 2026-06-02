using System.ComponentModel.DataAnnotations;

namespace Auth.Models;

public class RegisterViewModel
{
    [Required(ErrorMessage = "請輸入電子信箱")]
    [EmailAddress(ErrorMessage = "請輸入正確的電子信箱")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "請設定密碼")]
    [ValidPassword]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "請再次輸入密碼")]
    [Compare("Password", ErrorMessage = "兩次密碼不一致")]
    public string ConfirmPassword { get; set; } = "";

    [MustBeTrue(ErrorMessage = "請先同意服務條款")]
    public bool Agree { get; set; }
}
