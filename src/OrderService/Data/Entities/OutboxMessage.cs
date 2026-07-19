using Shared.Audit;
using Shared.Outbox;

namespace OrderService.Data.Entities;

/// <summary>Outbox 訊息；業務 transaction 內寫入，由 OutboxRelayService 批次推送至 MQ。</summary>
public class OutboxMessage : ICreatedAt, IOutboxMessage
{
    /// <summary>訊息唯一識別碼，同時作為事件的 OutboxMessageId。</summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>事件類型，如 order.completed、audit.order.create。</summary>
    public string EventType { get; set; } = "";

    /// <summary>JSON 序列化的事件 payload。</summary>
    public string Payload { get; set; } = "";

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>訊息推送至 MQ 的時間；null 表示尚未處理。</summary>
    public DateTimeOffset? ProcessedAt { get; set; }

    /// <summary>推送失敗累計次數，供指數退避與毒訊息隔離判斷。</summary>
    public int AttemptCount { get; set; }

    /// <summary>失敗退避後下次可重試的時間；null 表示可立即處理。</summary>
    public DateTimeOffset? NextAttemptAt { get; set; }

    /// <summary>反序列化失敗達上限被隔離的時間；非 null 者不再撈取，需人工介入。</summary>
    public DateTimeOffset? FailedAt { get; set; }

    /// <summary>最近一次失敗的錯誤訊息，成功後清除。</summary>
    public string? LastError { get; set; }
}
