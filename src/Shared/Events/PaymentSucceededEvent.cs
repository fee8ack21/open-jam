namespace Shared.Events;

/// <summary>
/// 付款成功事件，由 PaymentService 在 Stripe Webhook 處理完成後經 Outbox 發出。
/// CatalogService 等服務消費此事件以執行訂單 fulfillment。
/// </summary>
public record PaymentSucceededEvent(
    /// <summary>Outbox 訊息 ID，作為冪等去重鍵。</summary>
    Guid OutboxMessageId,
    /// <summary>付款紀錄 ID。</summary>
    Guid PaymentId,
    /// <summary>商品訂單 ID。</summary>
    Guid OrderId,
    /// <summary>購買者使用者 ID；null 表示以 Email 匿名購買。</summary>
    Guid? UserId,
    /// <summary>購買者電子信箱。</summary>
    string Email,
    /// <summary>付款金額（最低貨幣單位）。</summary>
    long Amount,
    /// <summary>貨幣代碼（小寫）。</summary>
    string Currency,
    /// <summary>Stripe PaymentIntent ID。</summary>
    string ProviderPaymentId,
    /// <summary>付款成功時間（UTC）。</summary>
    DateTimeOffset PaidAt
);
