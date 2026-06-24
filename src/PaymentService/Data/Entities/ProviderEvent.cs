using Shared.Audit;

namespace PaymentService.Data.Entities;

/// <summary>第三方金流事件（Webhook）去重紀錄。</summary>
public class ProviderEvent : ICreatedAt
{
    public Guid Id { get; set; }

    /// <summary>金流提供商（"stripe"）。</summary>
    public string Provider { get; set; } = "";

    /// <summary>事件 ID（Stripe Event ID，如 "evt_xxx"），作為冪等鍵。</summary>
    public string EventId { get; set; } = "";

    /// <summary>事件類型（如 "checkout.session.completed"）。</summary>
    public string EventType { get; set; } = "";

    /// <summary>原始 webhook payload（JSON 字串），供 BackgroundService 非同步處理；接收時已驗證簽章。</summary>
    public string RawPayload { get; set; } = "";

    /// <summary>處理完成時間（UTC）；null 表示尚未處理完畢，由 <c>StripeWebhookProcessorService</c> 排程處理。</summary>
    public DateTimeOffset? ProcessedAt { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; private set; }
}
