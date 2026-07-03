using Shared.Audit;

namespace NotificationService.Data.Entities;

/// <summary>
/// 已消費事件紀錄（inbox 去重）。Consumer 在處理事件前先以 <see cref="OutboxMessageId"/>
/// INSERT 一筆 claim，撞唯一索引即代表重複投遞、冪等跳過。
/// </summary>
public class ProcessedEvent : ICreatedAt
{
    /// <summary>主鍵。</summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>來源 Outbox 訊息 ID（去重鍵，唯一）。</summary>
    public Guid OutboxMessageId { get; set; }

    /// <summary>事件類型（供診斷用）。</summary>
    public string EventType { get; set; } = "";

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; private set; }
}
