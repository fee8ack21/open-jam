using Shared.Audit;

namespace LogService.Data.Entities;

/// <summary>
/// 稽核事件紀錄。Append-only：應用層只 INSERT，不開放 UPDATE / DELETE。
/// </summary>
public class AuditLog : ICreatedAt
{
    /// <summary>紀錄唯一識別碼。</summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>來源 Outbox 訊息 ID，同時作為冪等去重鍵（unique index）。</summary>
    public Guid OutboxMessageId { get; set; }

    /// <summary>操作者 user ID；null 表示系統自動動作。</summary>
    public Guid? Who { get; set; }

    /// <summary>動作類型（例如 "auth.login.success"）。</summary>
    public string Action { get; set; } = "";

    /// <summary>操作對象資源類型（例如 "User"）。</summary>
    public string Target { get; set; } = "";

    /// <summary>操作對象資源 ID。</summary>
    public Guid? TargetId { get; set; }

    /// <summary>動作結果："success" 或 "failure"。</summary>
    public string Result { get; set; } = "";

    /// <summary>變更前狀態（JSON 字串）；null 表示非修改類動作。</summary>
    public string? Before { get; set; }

    /// <summary>變更後狀態（JSON 字串）；null 表示非修改類動作。</summary>
    public string? After { get; set; }

    /// <summary>操作者 IP 位址。</summary>
    public string? Ip { get; set; }

    /// <summary>User-Agent 字串。</summary>
    public string? UserAgent { get; set; }

    /// <summary>所屬租戶（creator）ID；null 表示平台層級動作。</summary>
    public Guid? Tenant { get; set; }

    /// <summary>業務事件實際發生時間（UTC），來自事件 OccurredAt 欄位。</summary>
    public DateTimeOffset OccurredAt { get; set; }

    /// <summary>跨服務追蹤用的 correlation / trace ID。</summary>
    public string? CorrelationId { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; private set; }
}
