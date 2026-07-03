using Shared.Audit;

namespace NotificationService.Data.Entities;

/// <summary>
/// 通知任務。所有通知（即時與預定日期）統一為一筆 request，
/// 由 NotificationDispatcherService 掃描到期的 Pending 任務對商店追蹤者 fan-out。
/// </summary>
public class NotificationRequest : ICreatedAt, IUpdatedAt
{
    /// <summary>通知任務唯一識別碼。</summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>通知類型（如 "catalog.published"、"store.announcement"），決定模板與 fan-out 行為。</summary>
    public string Type { get; set; } = "";

    /// <summary>通知對象商店 ID（fan-out 至該商店的追蹤者）。</summary>
    public Guid StoreId { get; set; }

    /// <summary>通知內容參數（JSON，依 Type 決定結構）。</summary>
    public string Payload { get; set; } = "";

    /// <summary>預定發送時間；即時通知為建立當下。</summary>
    public DateTimeOffset ScheduledAt { get; set; }

    /// <summary>任務狀態。</summary>
    public NotificationRequestStatus Status { get; set; } = NotificationRequestStatus.Pending;

    /// <summary>完成 fan-out 的時間；null 表示尚未發送。</summary>
    public DateTimeOffset? DispatchedAt { get; set; }

    /// <summary>來源事件的 OutboxMessageId（消費事件建立時的冪等鍵，唯一）；API 建立者為 null。</summary>
    public Guid? SourceEventId { get; set; }

    /// <summary>發送嘗試次數；達上限後轉為 Failed。</summary>
    public int AttemptCount { get; set; }

    /// <summary>最近一次發送失敗原因；成功後清空。</summary>
    public string? LastError { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <inheritdoc/>
    public DateTimeOffset? UpdatedAt { get; private set; }
}
