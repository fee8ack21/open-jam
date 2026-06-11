using Auth.Models;
using Auth.Options;
using Auth.Services.Hydra;
using Auth.Services.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Auth.Controllers;

/// <summary>Auth service 的主要 MVC Controller，處理登入、註冊、信箱驗證、忘記密碼及重置密碼流程。</summary>
public class HomeController(IHydraService hydra, IUserService userService, IOptions<AppOptions> appOptions) : Controller
{
    /// <summary>根路徑，導向登入頁。</summary>
    [HttpGet("/")]
    public IActionResult Index() => RedirectToAction(nameof(Login));

    // ── Login ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 登入頁 GET；若帶有 login_challenge 則向 Hydra 查詢，
    /// skip = true 時直接接受（Hydra 已有 session）。
    /// 無 challenge 時導向 Workspace，由其發起 OIDC flow。
    /// </summary>
    [HttpGet("login")]
    public async Task<IActionResult> Login(string? login_challenge)
    {
        if (string.IsNullOrEmpty(login_challenge))
            return Redirect(appOptions.Value.WorkspaceUrl);

        var info = await hydra.GetLoginInfoAsync(login_challenge);

        if (info.Skip)
        {
            var redirect = await hydra.AcceptLoginAsync(login_challenge,
                new HydraAcceptLoginRequest(info.Subject, Remember: true, RememberFor: 3600));
            return Redirect(redirect);
        }

        return View(new LoginViewModel { Challenge = login_challenge });
    }

    /// <summary>登入 POST；驗證帳號密碼，成功後回應 Hydra 或導向完成頁。</summary>
    [HttpPost("login"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var (success, subject, error) = await userService.LoginAsync(model.Email, model.Password);
        if (!success)
        {
            ModelState.AddModelError("", error!);
            return View(model);
        }

        var redirect = await hydra.AcceptLoginAsync(model.Challenge!,
            new HydraAcceptLoginRequest(
                Subject:     subject!,
                Remember:    model.Remember,
                RememberFor: model.Remember ? 3600 * 24 * 30 : 0));
        return Redirect(redirect);
    }

    // ── Consent (auto-accept for first-party app) ─────────────────────────────

    /// <summary>Hydra consent 端點，第一方應用自動全部接受。</summary>
    [HttpGet("consent")]
    public async Task<IActionResult> Consent(string consent_challenge)
    {
        var info = await hydra.GetConsentInfoAsync(consent_challenge);

        var accessTokenClaims = await userService.GetAccessTokenClaimsAsync(info.Subject);

        var redirect = await hydra.AcceptConsentAsync(consent_challenge,
            new HydraAcceptConsentRequest(
                GrantScope:    info.RequestedScope,
                GrantAudience: info.RequestedAudience,
                Remember:      true,
                RememberFor:   3600,
                Session:       new HydraConsentSession(IdToken: null, AccessToken: accessTokenClaims)));
        return Redirect(redirect);
    }

    // ── Logout ────────────────────────────────────────────────────────────────

    /// <summary>Hydra logout 端點，接受登出並導向 Hydra 指定 URL。</summary>
    [HttpGet("logout")]
    public async Task<IActionResult> Logout(string? logout_challenge)
    {
        if (string.IsNullOrEmpty(logout_challenge))
            return RedirectToAction(nameof(Login));

        var redirect = await hydra.AcceptLogoutAsync(logout_challenge);
        return Redirect(redirect);
    }

    // ── Register ──────────────────────────────────────────────────────────────

    /// <summary>註冊頁 GET。</summary>
    [HttpGet("register")]
    public IActionResult Register(string? login_challenge) =>
        View(new RegisterViewModel { LoginChallenge = login_challenge });

    /// <summary>
    /// 註冊 POST；建立帳號並寄發驗證信。
    /// 若同一信箱已有 Pending 帳號（squatting）則覆蓋並重發驗證信。
    /// </summary>
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

    /// <summary>驗證信已寄出確認頁。</summary>
    [HttpGet("register-sent")]
    public IActionResult RegisterSent()
    {
        var email = TempData["Email"] as string;
        if (string.IsNullOrEmpty(email))
            return RedirectToAction(nameof(Login));
        ViewBag.Email = email;
        return View();
    }

    // ── Email verification ────────────────────────────────────────────────────

    /// <summary>
    /// 信箱驗證端點；處理驗證 token，成功後導向完成頁，失敗導向錯誤頁。
    /// </summary>
    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmail(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction(nameof(VerifyFailed));

        var (success, _) = await userService.VerifyEmailAsync(token);
        return success
            ? RedirectToAction(nameof(VerifyDone))
            : RedirectToAction(nameof(VerifyFailed));
    }

    /// <summary>信箱驗證成功頁。</summary>
    [HttpGet("verify-done")]
    public IActionResult VerifyDone() => View();

    /// <summary>信箱驗證失敗頁（連結無效、過期或已使用）。</summary>
    [HttpGet("verify-failed")]
    public IActionResult VerifyFailed() => View();

    // ── Forgot password ───────────────────────────────────────────────────────

    /// <summary>忘記密碼頁 GET。</summary>
    [HttpGet("forgot")]
    public IActionResult ForgotPassword(string? login_challenge) =>
        View(new ForgotPasswordViewModel { LoginChallenge = login_challenge });

    /// <summary>忘記密碼 POST；寄出重置信（無論信箱是否存在皆回同一畫面，防帳號列舉）。</summary>
    [HttpPost("forgot"), ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        await userService.SendPasswordResetAsync(model.Email);
        TempData["Email"] = model.Email;
        return RedirectToAction(nameof(ForgotSent));
    }

    /// <summary>重置密碼信已寄出確認頁。</summary>
    [HttpGet("forgot-sent")]
    public IActionResult ForgotSent()
    {
        ViewBag.Email = TempData["Email"] as string ?? "";
        return View();
    }

    // ── Reset password ────────────────────────────────────────────────────────

    /// <summary>重置密碼頁 GET；從 query string 取得 token 並注入 ViewModel。</summary>
    [HttpGet("reset")]
    public IActionResult ResetPassword(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction(nameof(VerifyFailed));

        return View(new ResetPasswordViewModel { Token = token });
    }

    /// <summary>重置密碼 POST；驗證 token 並更新密碼，成功後導向完成頁。</summary>
    [HttpPost("reset"), ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var (success, error) = await userService.ResetPasswordAsync(model.Token, model.Password);
        if (!success)
        {
            ModelState.AddModelError("", error!);
            return View(model);
        }

        return RedirectToAction(nameof(ResetDone));
    }

    /// <summary>密碼重置完成頁。</summary>
    [HttpGet("reset-done")]
    public IActionResult ResetDone() => View();

    /// <summary>錯誤頁。</summary>
    [HttpGet("error")]
    public IActionResult Error() => View();
}
