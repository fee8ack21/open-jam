using Auth.Models;
using Auth.Services;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Controllers;

public class HomeController(IHydraService hydra, IUserService userService) : Controller
{
    [HttpGet("/")]
    public IActionResult Index() => RedirectToAction(nameof(Login));

    // ── Login ─────────────────────────────────────────────────────────────────

    [HttpGet("login")]
    public async Task<IActionResult> Login(string? login_challenge)
    {
        if (string.IsNullOrEmpty(login_challenge))
            return View(new LoginViewModel());

        var info = await hydra.GetLoginInfoAsync(login_challenge);

        if (info.Skip)
        {
            var redirect = await hydra.AcceptLoginAsync(login_challenge,
                new HydraAcceptLoginRequest(info.Subject, Remember: true, RememberFor: 3600));
            return Redirect(redirect);
        }

        return View(new LoginViewModel { Challenge = login_challenge });
    }

    [HttpPost("login"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        // TODO: authenticate against real user store

        if (!string.IsNullOrEmpty(model.Challenge))
        {
            var redirect = await hydra.AcceptLoginAsync(model.Challenge,
                new HydraAcceptLoginRequest(
                    Subject:     model.Email,
                    Remember:    model.Remember,
                    RememberFor: model.Remember ? 3600 * 24 * 30 : 0));
            return Redirect(redirect);
        }

        TempData["Email"] = model.Email;
        return RedirectToAction(nameof(LoginDone));
    }

    [HttpGet("login-done")]
    public IActionResult LoginDone()
    {
        ViewBag.Email = TempData["Email"] as string ?? "";
        return View();
    }

    // ── Consent (auto-accept for first-party app) ─────────────────────────────

    [HttpGet("consent")]
    public async Task<IActionResult> Consent(string consent_challenge)
    {
        var info = await hydra.GetConsentInfoAsync(consent_challenge);
        var redirect = await hydra.AcceptConsentAsync(consent_challenge,
            new HydraAcceptConsentRequest(
                GrantScope:    info.RequestedScope,
                GrantAudience: info.RequestedAudience,
                Remember:      true,
                RememberFor:   3600,
                Session:       new HydraConsentSession(IdToken: null)));
        return Redirect(redirect);
    }

    // ── Logout ────────────────────────────────────────────────────────────────

    [HttpGet("logout")]
    public async Task<IActionResult> Logout(string? logout_challenge)
    {
        if (string.IsNullOrEmpty(logout_challenge))
            return RedirectToAction(nameof(Login));

        var redirect = await hydra.AcceptLogoutAsync(logout_challenge);
        return Redirect(redirect);
    }

    // ── Register ──────────────────────────────────────────────────────────────

    [HttpGet("register")]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost("register"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var (success, error) = await userService.RegisterAsync(model.Email, model.Password);
        if (!success)
        {
            ModelState.AddModelError(nameof(model.Email), error!);
            return View(model);
        }

        TempData["Email"] = model.Email;
        return RedirectToAction(nameof(RegisterSent));
    }

    [HttpGet("register-sent")]
    public IActionResult RegisterSent()
    {
        ViewBag.Email = TempData["Email"] as string ?? "";
        return View();
    }

    // ── Forgot password ───────────────────────────────────────────────────────

    [HttpGet("forgot")]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [HttpPost("forgot"), ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        await userService.SendPasswordResetAsync(model.Email);
        TempData["Email"] = model.Email;
        return RedirectToAction(nameof(ForgotSent));
    }

    [HttpGet("forgot-sent")]
    public IActionResult ForgotSent()
    {
        ViewBag.Email = TempData["Email"] as string ?? "";
        return View();
    }

    // ── Reset password ────────────────────────────────────────────────────────

    [HttpGet("reset")]
    public IActionResult ResetPassword() => View(new ResetPasswordViewModel());

    [HttpPost("reset"), ValidateAntiForgeryToken]
    public IActionResult ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        // TODO: validate token + update password
        return RedirectToAction(nameof(ResetDone));
    }

    [HttpGet("reset-done")]
    public IActionResult ResetDone() => View();

    [HttpGet("error")]
    public IActionResult Error() => View();
}
