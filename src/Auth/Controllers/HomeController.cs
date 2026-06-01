using Microsoft.AspNetCore.Mvc;

namespace Auth.Controllers
{
    public class HomeController : Controller
    {
        [HttpGet("/")]
        public IActionResult Index() => RedirectToAction(nameof(Login));

        [HttpGet("login")]
        public IActionResult Login() => View();

        [HttpGet("register")]
        public IActionResult Register() => View();

        [HttpGet("forgot")]
        public IActionResult ForgotPassword() => View();

        [HttpGet("reset")]
        public IActionResult ResetPassword() => View();

        [HttpGet("error")]
        public IActionResult Error() => View();
    }
}
