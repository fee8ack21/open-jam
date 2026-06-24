using Shared.Audit;

namespace PaymentService.Data.Entities;

/// <summary>付款紀錄，對應一筆 Stripe PaymentIntent。</summary>
public class Payment : ICreatedAt, IUpdatedAt
{
    public Guid Id { get; set; }

    /// <summary>對應的商品訂單 ID（由 CatalogService 產生）。</summary>
    public Guid OrderId { get; set; }

    /// <summary>購買者使用者 ID（已登入用戶）；null 表示以 Email 匿名購買。</summary>
    public Guid? UserId { get; set; }

    /// <summary>購買者電子信箱（匿名購買必填）。</summary>
    public string Email { get; set; } = "";

    /// <summary>金流提供商（固定 "stripe"）。</summary>
    public string Provider { get; set; } = "stripe";

    /// <summary>付款金額（最低貨幣單位，如 cents）。</summary>
    public long Amount { get; set; }

    /// <summary>貨幣代碼（小寫，如 "usd"、"twd"）。</summary>
    public string Currency { get; set; } = "usd";

    /// <summary>付款狀態。</summary>
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    /// <summary>第三方金流的付款編號（Stripe PaymentIntent ID）。</summary>
    public string? ProviderPaymentId { get; set; }

    /// <summary>第三方金流的結帳階段 ID（Stripe CheckoutSession ID）。</summary>
    public string? ProviderCheckoutId { get; set; }

    /// <summary>Stripe Checkout 頁面網址，供同一訂單重複請求時直接重用，避免建立重複 Session。</summary>
    public string? CheckoutUrl { get; set; }

    /// <summary>Stripe Checkout Session 到期時間（UTC），到期前同一訂單的請求會重用此筆付款。</summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <inheritdoc/>
    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>付款成功時間（UTC）。</summary>
    public DateTimeOffset? PaidAt { get; set; }

    /// <summary>付款失敗時間（UTC）。</summary>
    public DateTimeOffset? FailedAt { get; set; }

    /// <summary>使用者未完成付款、Stripe Checkout Session 過期時間（UTC）。</summary>
    public DateTimeOffset? ExpiredAt { get; set; }
}
