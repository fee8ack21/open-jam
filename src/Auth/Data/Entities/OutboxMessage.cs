using Shared.Audit;

namespace Auth.Data.Entities;

/// <summary>Outbox 訊息；業務 transaction 內寫入，由 OutboxRelayService 批次推送至 MQ。</summary>
public class OutboxMessage : ICreatedAt
{
    /// <summary>訊息唯一識別碼，同時作為 EmailRequestedEvent 的 OutboxMessageId。</summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>事件類型，如 email.verification、email.password_reset。</summary>
    public string EventType { get; set; } = "";

    /// <summary>JSON 序列化的事件 payload。</summary>
    public string Payload { get; set; } = "";

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>訊息推送至 MQ 的時間；null 表示尚未處理。</summary>
    public DateTimeOffset? ProcessedAt { get; set; }
}
