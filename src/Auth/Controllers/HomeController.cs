using Auth.Models;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Controllers
{
    public class HomeController : Controller
    {
        [HttpGet("/")]
        public IActionResult Index() => RedirectToAction(nameof(Login));

        [HttpGet("login")]
        public IActionResult Login() => View(new LoginViewModel());

        [HttpPost("login"), ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            // TODO: real authentication
            TempData["Email"] = model.Email;
            return RedirectToAction(nameof(LoginDone));
        }

        [HttpGet("login-done")]
        public IActionResult LoginDone()
        {
            ViewBag.Email = TempData["Email"] as string ?? "";
            return View();
        }

        [HttpGet("register")]
        public IActionResult Register() => View(new RegisterViewModel());

        [HttpPost("register"), ValidateAntiForgeryToken]
        public IActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            // TODO: real registration
            TempData["Email"] = model.Email;
            return RedirectToAction(nameof(RegisterSent));
        }

        [HttpGet("register-sent")]
        public IActionResult RegisterSent()
        {
            ViewBag.Email = TempData["Email"] as string ?? "";
            return View();
        }

        [HttpGet("forgot")]
        public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

        [HttpPost("forgot"), ValidateAntiForgeryToken]
        public IActionResult ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            // TODO: real forgot-password flow
            TempData["Email"] = model.Email;
            return RedirectToAction(nameof(ForgotSent));
        }

        [HttpGet("forgot-sent")]
        public IActionResult ForgotSent()
        {
            ViewBag.Email = TempData["Email"] as string ?? "";
            return View();
        }

        [HttpGet("reset")]
        public IActionResult ResetPassword() => View(new ResetPasswordViewModel());

        [HttpPost("reset"), ValidateAntiForgeryToken]
        public IActionResult ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            // TODO: real password reset
            return RedirectToAction(nameof(ResetDone));
        }

        [HttpGet("reset-done")]
        public IActionResult ResetDone() => View();

        [HttpGet("error")]
        public IActionResult Error() => View();
    }
}
