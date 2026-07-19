namespace Auth.Services.Security;

/// <summary>Cloudflare Turnstile 人機驗證。</summary>
public interface ITurnstileService
{
    /// <summary>
    /// 向 Cloudflare siteverify 驗證表單回傳的 token（單次有效、300 秒過期）。
    /// 未設定金鑰（停用）時一律通過；siteverify 連線失敗採 fail-open 放行
    /// （帳號鎖定與 IP rate limit 仍為底線防護）。
    /// </summary>
    Task<bool> VerifyAsync(string? token, string? remoteIp);
}
