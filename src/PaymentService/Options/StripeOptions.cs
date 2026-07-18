namespace PaymentService.Options;

/// <summary>Stripe 金流設定，對應 appsettings <c>Stripe</c> 區段。</summary>
public class StripeOptions
{
    public string SecretKey { get; set; } = "";
    public string PublishableKey { get; set; } = "";
    public string WebhookSecret { get; set; } = "";

    /// <summary>Connect webhook 端點簽章密鑰（account.updated 等 Connect 事件與平台事件分屬不同端點）。</summary>
    public string ConnectWebhookSecret { get; set; } = "";

    /// <summary>平台抽成百分比（destination charge 的 application fee 的百分比部分），如 10 表示 10%。</summary>
    public decimal PlatformFeePercent { get; set; }

    /// <summary>
    /// 平台抽成固定費（application fee 的固定部分），單位為最低貨幣單位（與 <c>Amount</c> 一致，
    /// 如 1200 表示 NT$12）。用以吸收 Stripe 每筆固定手續費（$0.30），使低單價訂單也不虧本。
    /// </summary>
    public long PlatformFeeFixed { get; set; }

    /// <summary>Stripe onboarding Account Link 失效重新導向 URL（workspace-web 收款設定頁）。</summary>
    public string ConnectRefreshUrl { get; set; } = "";

    /// <summary>Stripe onboarding 完成後導回 URL（workspace-web 收款設定頁）。</summary>
    public string ConnectReturnUrl { get; set; } = "";

    /// <summary>
    /// 付款成功導向 URL（模板）；<c>{CHECKOUT_SESSION_ID}</c> 由 Stripe 代入，
    /// <c>{storeSlug}</c>（若有）由 PaymentService 以該訂單所屬商店子網域代稱代入
    /// （結帳結果頁位於店面子網域）。
    /// </summary>
    public string SuccessUrl { get; set; } = "";

    /// <summary>取消付款導向 URL（模板）；<c>{storeSlug}</c>（若有）同 <see cref="SuccessUrl"/> 代入。</summary>
    public string CancelUrl { get; set; } = "";
}
