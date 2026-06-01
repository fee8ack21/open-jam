using System.ComponentModel.DataAnnotations;

namespace Auth.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "請輸入電子信箱")]
    [EmailAddress(ErrorMessage = "請輸入正確的電子信箱")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "請輸入密碼")]
    public string Password { get; set; } = "";

    public bool Remember { get; set; } = true;
}
