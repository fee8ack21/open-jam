namespace NotificationService.Data.Entities;

/// <summary>通知任務狀態。</summary>
public enum NotificationRequestStatus
{
    /// <summary>待發送（含尚未到期的預定通知）。</summary>
    Pending = 0,

    /// <summary>已完成 fan-out。</summary>
    Dispatched = 1,

    /// <summary>已取消（僅 Pending 可取消）。</summary>
    Cancelled = 2,

    /// <summary>重試次數用盡，放棄發送。</summary>
    Failed = 3,
}
