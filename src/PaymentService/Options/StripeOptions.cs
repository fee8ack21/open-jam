namespace PaymentService.Options;

/// <summary>Stripe 金流設定，對應 appsettings <c>Stripe</c> 區段。</summary>
public class StripeOptions
{
    public string SecretKey { get; set; } = "";
    public string PublishableKey { get; set; } = "";
    public string WebhookSecret { get; set; } = "";

    /// <summary>付款成功導向 URL（模板）；<c>{CHECKOUT_SESSION_ID}</c> 由 Stripe 代入。</summary>
    public string SuccessUrl { get; set; } = "";

    /// <summary>取消付款導向 URL。</summary>
    public string CancelUrl { get; set; } = "";
}
