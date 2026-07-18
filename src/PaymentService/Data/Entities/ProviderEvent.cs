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

    /// <summary>處理失敗次數；達上限後標記 <see cref="FailedAt"/> 出列，避免毒事件永久佔據處理批次。</summary>
    public int AttemptCount { get; set; }

    /// <summary>最後一次處理失敗的錯誤內容（截斷保留），供事後排查 dead-letter 事件。</summary>
    public string? LastError { get; set; }

    /// <summary>處理失敗放棄時間（UTC，dead-letter）；非 null 者不再重試，需人工排查後清除此欄位重新入列。</summary>
    public DateTimeOffset? FailedAt { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; private set; }
}
