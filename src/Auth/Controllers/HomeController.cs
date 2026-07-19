using System.Text.Json;
using Auth.Common;
using Auth.Data.Entities;
using Auth.Models;
using Auth.Options;
using Auth.Services.Hydra;
using Auth.Services.Legal;
using Auth.Services.Security;
using Auth.Services.Users;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Auth.Controllers;

/// <summary>Auth service 的主要 MVC Controller，處理登入、註冊、信箱驗證、忘記密碼及重置密碼流程。</summary>
/// <remarks>純 MVC 視圖流程，非 REST API；以 IgnoreApi 排除於 OpenAPI 文件外。</remarks>
[ApiExplorerSettings(IgnoreApi = true)]
[InvalidChallengeRedirect]
public class HomeController(
    IHydraService hydra,
    IUserService userService,
    ILegalConsentService legalConsentService,
    ITurnstileService turnstileService,
    IDataProtectionProvider dataProtection,
    IOptions<AppOptions> appOptions) : Controller
{
    /// <summary>Hydra session / consent 的記住時長（30 天），login 與 consent 端點共用。</summary>
    private const int RememberForSeconds = 3600 * 24 * 30;

    /// <summary>re-consent 登入票證有效時間；逾時須重新登入。</summary>
    private static readonly TimeSpan ReconsentTicketLifetime = TimeSpan.FromMinutes(15);

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
            // Hydra session 尚存的自動登入路徑也要檢查是否有新版條款未同意
            if (await BuildReconsentResultAsync(info.Subject, login_challenge, remember: true) is { } reconsent)
                return reconsent;

            var redirect = await hydra.AcceptLoginAsync(login_challenge,
                new HydraAcceptLoginRequest(info.Subject, Remember: true, RememberFor: RememberForSeconds));
            return Redirect(redirect);
        }

        return View(new LoginViewModel { Challenge = login_challenge });
    }

    /// <summary>登入 POST；驗證帳號密碼，成功後回應 Hydra 或導向完成頁。</summary>
    [HttpPost("login"), ValidateAntiForgeryToken, EnableRateLimiting("auth-form")]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        // challenge 遺失（過期頁面重送、直接 POST）時無法回應 Hydra，導向 Workspace 重新發起 OIDC flow
        if (string.IsNullOrEmpty(model.Challenge))
            return Redirect(appOptions.Value.WorkspaceUrl);

        if (!ModelState.IsValid) return View(model);
        if (!await VerifyCaptchaAsync()) return View(model);

        var (success, subject, error) = await userService.LoginAsync(model.Email, model.Password);
        if (!success)
        {
            ModelState.AddModelError("", error!);
            return View(model);
        }

        // 有新版（未同意過的）啟用條款時，先重新勾選同意才完成登入
        if (await BuildReconsentResultAsync(subject!, model.Challenge, model.Remember) is { } reconsent)
            return reconsent;

        var redirect = await hydra.AcceptLoginAsync(model.Challenge,
            new HydraAcceptLoginRequest(
                Subject:     subject!,
                Remember:    model.Remember,
                RememberFor: model.Remember ? RememberForSeconds : 0));
        return Redirect(redirect);
    }

    // ── Consent (auto-accept for first-party app) ─────────────────────────────

    /// <summary>Hydra consent 端點，第一方應用自動全部接受。</summary>
    [HttpGet("consent")]
    public async Task<IActionResult> Consent(string? consent_challenge)
    {
        // consent 頁只該由 Hydra 帶 challenge 導入，缺 challenge 即視為非法存取
        if (string.IsNullOrEmpty(consent_challenge))
            return RedirectToAction(nameof(Error));

        var info = await hydra.GetConsentInfoAsync(consent_challenge);

        var accessTokenClaims = await userService.GetAccessTokenClaimsAsync(info.Subject);

        var redirect = await hydra.AcceptConsentAsync(consent_challenge,
            new HydraAcceptConsentRequest(
                GrantScope:    info.RequestedScope,
                GrantAudience: info.RequestedAudience,
                Remember:      true,
                RememberFor:   RememberForSeconds,
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

    /// <summary>註冊頁 GET；載入目前啟用中的服務條款與隱私權政策供 dialog 呈現。</summary>
    [HttpGet("register")]
    public async Task<IActionResult> Register(string? login_challenge) =>
        View(await WithActiveLegalDocumentsAsync(new RegisterViewModel { LoginChallenge = login_challenge }));

    /// <summary>
    /// 註冊 POST；建立帳號、寫入條款同意紀錄（同意當下啟用中的版本）並寄發驗證信。
    /// 若同一信箱已有 Pending 帳號（squatting）則覆蓋並重發驗證信。
    /// </summary>
    [HttpPost("register"), ValidateAntiForgeryToken, EnableRateLimiting("auth-email")]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(await WithActiveLegalDocumentsAsync(model));
        if (!await VerifyCaptchaAsync()) return View(await WithActiveLegalDocumentsAsync(model));

        var (success, error) = await userService.RegisterAsync(model.Email, model.Password);
        if (!success)
        {
            ModelState.AddModelError(nameof(model.Email), error!);
            return View(await WithActiveLegalDocumentsAsync(model));
        }

        TempData["Email"] = model.Email;
        return RedirectToAction(nameof(RegisterSent));
    }

    // ── Legal re-consent（登入時新版條款重新同意）──────────────────────────────

    /// <summary>
    /// re-consent 同意 POST；驗證登入票證後寫入同意紀錄並完成 Hydra AcceptLogin。
    /// 票證無效或逾時（15 分鐘）導向 Workspace 重新發起登入。
    /// </summary>
    [HttpPost("legal-reconsent"), ValidateAntiForgeryToken]
    public async Task<IActionResult> LegalReconsent(LegalReconsentViewModel model)
    {
        ReconsentTicket? ticket;
        try
        {
            ticket = JsonSerializer.Deserialize<ReconsentTicket>(ReconsentProtector().Unprotect(model.Ticket));
        }
        catch
        {
            ticket = null;
        }

        if (ticket is null || !Guid.TryParse(ticket.Subject, out var userId))
            return Redirect(appOptions.Value.WorkspaceUrl);

        var pending = await legalConsentService.GetPendingConsentAsync(userId);

        if (!ModelState.IsValid)
        {
            model.Documents = pending;
            return View(model);
        }

        // 以伺服器當下重查的未同意清單為準寫入同意紀錄，不信任表單回傳的文件 ID
        await legalConsentService.RecordConsentsAsync(userId, pending.Select(d => d.Id).ToList());

        var redirect = await hydra.AcceptLoginAsync(ticket.Challenge,
            new HydraAcceptLoginRequest(
                Subject:     ticket.Subject,
                Remember:    ticket.Remember,
                RememberFor: ticket.Remember ? RememberForSeconds : 0));
        return Redirect(redirect);
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
    [HttpPost("forgot"), ValidateAntiForgeryToken, EnableRateLimiting("auth-email")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        if (!await VerifyCaptchaAsync()) return View(model);

        await userService.SendPasswordResetAsync(model.Email);
        TempData["Email"] = model.Email;
        return RedirectToAction(nameof(ForgotSent));
    }

    /// <summary>重置密碼信已寄出確認頁。</summary>
    [HttpGet("forgot-sent")]
    public IActionResult ForgotSent()
    {
        var email = TempData["Email"] as string;
        if (string.IsNullOrEmpty(email))
            return RedirectToAction(nameof(Login));
        ViewBag.Email = email;
        return View();
    }

    // ── Reset password ────────────────────────────────────────────────────────

    /// <summary>重置密碼頁 GET；從 query string 取得 token 並注入 ViewModel。</summary>
    [HttpGet("reset")]
    public IActionResult ResetPassword(string? token)
    {
        // token 遺失時導向忘記密碼頁重新申請（VerifyFailed 是信箱驗證情境，文案會誤導使用者去重新註冊）
        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction(nameof(ForgotPassword));

        return View(new ResetPasswordViewModel { Token = token });
    }

    /// <summary>重置密碼 POST；驗證 token 並更新密碼，成功後導向完成頁。</summary>
    [HttpPost("reset"), ValidateAntiForgeryToken, EnableRateLimiting("auth-form")]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        if (!await VerifyCaptchaAsync()) return View(model);

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

    /// <summary>
    /// 錯誤頁；Hydra 的 URLS_ERROR 導入時帶 error / error_description query，
    /// 顯示真實錯誤代碼與描述以利排查（如 redirect URI 不在白名單）。
    /// UseExceptionHandler 導入（無 query）時退回既有通用文案。
    /// </summary>
    [HttpGet("error")]
    public IActionResult Error(string? error, string? error_description)
    {
        ViewData["OidcError"] = error;
        ViewData["OidcErrorDescription"] = error_description;
        return View();
    }

    /// <summary>切換語系，設定 Cookie 後導回原頁。</summary>
    [HttpGet("set-language")]
    public IActionResult SetLanguage(string culture, string? returnUrl)
    {
        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), HttpOnly = true, SameSite = SameSiteMode.Lax });
        return LocalRedirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
    }

    // ── 私有輔助方法 ───────────────────────────────────────────────────────────

    /// <summary>將目前啟用中的服務條款與隱私權政策掛上註冊 ViewModel（GET 與 POST 失敗重顯共用）。</summary>
    private async Task<RegisterViewModel> WithActiveLegalDocumentsAsync(RegisterViewModel model)
    {
        var docs = await legalConsentService.GetActiveAsync(null);
        model.TermsDocument   = docs.FirstOrDefault(d => d.Type == LegalDocumentType.TermsOfService);
        model.PrivacyDocument = docs.FirstOrDefault(d => d.Type == LegalDocumentType.PrivacyPolicy);
        return model;
    }

    /// <summary>
    /// 檢查使用者是否有未同意的啟用中條款；有則回傳 re-consent 畫面（帶時效性登入票證），
    /// 無（或 subject 非使用者 GUID）則回傳 null 表示可直接完成登入。
    /// </summary>
    private async Task<IActionResult?> BuildReconsentResultAsync(string subject, string challenge, bool remember)
    {
        if (!Guid.TryParse(subject, out var userId))
            return null;

        var pending = await legalConsentService.GetPendingConsentAsync(userId);
        if (pending.Count == 0)
            return null;

        var ticket = ReconsentProtector().Protect(
            JsonSerializer.Serialize(new ReconsentTicket(subject, challenge, remember)),
            ReconsentTicketLifetime);

        return View("LegalReconsent", new LegalReconsentViewModel { Ticket = ticket, Documents = pending });
    }

    /// <summary>
    /// 驗證表單夾帶的 Turnstile token（widget 自動塞入 cf-turnstile-response 欄位）；
    /// 未通過時掛上表單層級錯誤，呼叫端據以重新渲染表單（widget 隨頁面重載重新出題）。
    /// </summary>
    private async Task<bool> VerifyCaptchaAsync()
    {
        var token = Request.Form["cf-turnstile-response"].ToString();
        if (await turnstileService.VerifyAsync(token, HttpContext.Connection.RemoteIpAddress?.ToString()))
            return true;

        ModelState.AddModelError("", "人機驗證未通過，請再試一次");
        return false;
    }

    /// <summary>re-consent 登入票證的 time-limited Data Protector。</summary>
    private ITimeLimitedDataProtector ReconsentProtector() =>
        dataProtection.CreateProtector("Auth.LegalReconsent").ToTimeLimitedDataProtector();
}
