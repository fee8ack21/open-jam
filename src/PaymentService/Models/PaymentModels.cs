using PaymentService.Data.Entities;

namespace PaymentService.Models;

/// <summary>建立 Stripe Checkout Session 請求。</summary>
public class CreateCheckoutSessionRequest
{
    /// <summary>商品訂單 ID。</summary>
    public Guid OrderId { get; set; }

    /// <summary>賣方商店 ID，用於查找分帳目的地 Stripe 帳戶。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid StoreId { get; set; }

    /// <summary>購買者使用者 ID；null 表示匿名購買。由 OrderService 帶入（呼叫者為內部服務，非買家本人）。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid? UserId { get; set; }

    /// <summary>購買者電子信箱。</summary>
    public string Email { get; set; } = "";

    /// <summary>付款金額（最低貨幣單位，如 cents）。</summary>
    public long Amount { get; set; }

    /// <summary>貨幣代碼（小寫，如 "usd"、"twd"）。</summary>
    public string Currency { get; set; } = "usd";

    /// <summary>商品名稱（顯示於 Stripe Checkout 頁面）。</summary>
    public string ProductName { get; set; } = "";
}

/// <summary>Checkout Session 建立回應。</summary>
public class CheckoutSessionResponse
{
    /// <summary>付款 ID。</summary>
    public Guid PaymentId { get; set; }

    /// <summary>Stripe Checkout Session ID。</summary>
    public string SessionId { get; set; } = "";

    /// <summary>Stripe Checkout URL（前端導向此 URL 完成付款）。</summary>
    public string Url { get; set; } = "";
}

/// <summary>付款紀錄回應。</summary>
public class PaymentResponse
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid StoreId { get; set; }
    public Guid? UserId { get; set; }
    public string Email { get; set; } = "";
    public string Provider { get; set; } = "";
    public long Amount { get; set; }
    public long ApplicationFeeAmount { get; set; }
    public string? DestinationAccountId { get; set; }
    public string Currency { get; set; } = "";
    public PaymentStatus Status { get; set; }
    public string? ProviderPaymentId { get; set; }
    public string? ProviderCheckoutId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public DateTimeOffset? FailedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? ExpiredAt { get; set; }
}
