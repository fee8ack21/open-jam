using System.ComponentModel.DataAnnotations;

namespace Auth.Models;

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "請輸入電子信箱")]
    [EmailAddress(ErrorMessage = "請輸入正確的電子信箱")]
    public string Email { get; set; } = "";
}
