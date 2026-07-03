using Shared.Audit;

namespace NotificationService.Data.Entities;

/// <summary>Outbox 訊息，業務 transaction 內寫入，由 OutboxRelayService 搬入 RabbitMQ。</summary>
public class OutboxMessage : ICreatedAt
{
    /// <summary>訊息唯一識別碼（下游消費端的冪等去重鍵）。</summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>事件類型（如 "email.notification"）。</summary>
    public string EventType { get; set; } = "";

    /// <summary>事件內容（JSON）。</summary>
    public string Payload { get; set; } = "";

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>已發布至 RabbitMQ 的時間；null 表示待處理。</summary>
    public DateTimeOffset? ProcessedAt { get; set; }
}
