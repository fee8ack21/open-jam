namespace Shared.Events;

/// <summary>
/// 各服務在業務 transaction 內寫入 Outbox 後，由排程搬入 RabbitMQ 的稽核事件。
/// LogService Consumer 負責消費並寫入 audit_log 資料表。
/// </summary>
public record AuditLogRequestedEvent(
    /// <summary>Outbox 訊息 ID，作為冪等去重鍵。</summary>
    Guid OutboxMessageId,
    /// <summary>操作者 user ID；null 表示系統自動動作。</summary>
    Guid? Who,
    /// <summary>動作類型（例如 "auth.login.success"、"auth.register"）。</summary>
    string Action,
    /// <summary>操作對象資源類型（例如 "User"、"Product"）。</summary>
    string Target,
    /// <summary>操作對象資源 ID；null 表示無特定資源（例如登入嘗試）。</summary>
    Guid? TargetId,
    /// <summary>動作結果：success 或 failure。</summary>
    string Result,
    /// <summary>變更前狀態（JSON 字串）；null 表示非修改類動作。</summary>
    string? Before,
    /// <summary>變更後狀態（JSON 字串）；null 表示非修改類動作。</summary>
    string? After,
    /// <summary>操作者 IP 位址。</summary>
    string? Ip,
    /// <summary>User-Agent 字串。</summary>
    string? UserAgent,
    /// <summary>所屬租戶（creator）ID；null 表示平台層級動作。</summary>
    Guid? Tenant,
    /// <summary>業務事件實際發生時間（UTC）。</summary>
    DateTimeOffset OccurredAt,
    /// <summary>跨服務追蹤用的 correlation / trace ID。</summary>
    string? CorrelationId
);
