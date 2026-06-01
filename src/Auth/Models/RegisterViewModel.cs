using System.ComponentModel.DataAnnotations;

namespace Auth.Models;

public class RegisterViewModel
{
    [Required(ErrorMessage = "請輸入你的暱稱")]
    public string DisplayName { get; set; } = "";

    [Required(ErrorMessage = "請輸入電子信箱")]
    [EmailAddress(ErrorMessage = "請輸入正確的電子信箱")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "請設定密碼")]
    [MinLength(8, ErrorMessage = "密碼至少需要 8 個字元")]
    public string Password { get; set; } = "";

    [MustBeTrue(ErrorMessage = "請先同意服務條款")]
    public bool Agree { get; set; }
}
