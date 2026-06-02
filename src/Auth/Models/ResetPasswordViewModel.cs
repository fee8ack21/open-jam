using System.ComponentModel.DataAnnotations;

namespace Auth.Models;

public class ResetPasswordViewModel
{
    [Required(ErrorMessage = "請設定新密碼")]
    [ValidPassword]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "請再次輸入密碼")]
    [Compare("Password", ErrorMessage = "兩次輸入的密碼不一致")]
    public string ConfirmPassword { get; set; } = "";
}
