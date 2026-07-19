namespace Auth.Options;

/// <summary>登入安全設定：帳號鎖定、寄信節流與表單 rate limit。</summary>
public class SecurityOptions
{
    /// <summary>連續登入失敗達此次數即暫時鎖定帳號。</summary>
    /// <example>5</example>
    public int MaxFailedLoginAttempts { get; set; } = 5;

    /// <summary>帳號暫時鎖定時長（分鐘），期滿自動解鎖並歸零計數。</summary>
    /// <example>15</example>
    public int LockoutMinutes { get; set; } = 15;

    /// <summary>同一帳號兩次觸發寄信（驗證信 / 重置信）間的冷卻秒數（mail-bomb 防護）。</summary>
    /// <example>60</example>
    public int EmailCooldownSeconds { get; set; } = 60;

    /// <summary>同一帳號一小時內可觸發寄信（驗證信 / 重置信）的次數上限（mail-bomb 防護）。</summary>
    /// <example>5</example>
    public int MaxEmailsPerHour { get; set; } = 5;

    /// <summary>單一 IP 於視窗內可送出認證表單（登入 / 重置）POST 的次數上限。</summary>
    /// <example>10</example>
    public int FormRateLimit { get; set; } = 10;

    /// <summary>單一 IP 於視窗內可送出寄信類表單（註冊 / 忘記密碼）POST 的次數上限。</summary>
    /// <example>5</example>
    public int EmailFormRateLimit { get; set; } = 5;

    /// <summary>IP rate limit 的固定視窗長度（秒）。</summary>
    /// <example>60</example>
    public int RateLimitWindowSeconds { get; set; } = 60;
}
