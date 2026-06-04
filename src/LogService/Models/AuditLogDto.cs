namespace LogService.Models;

/// <summary>
/// 單筆稽核事件回應。
/// 注意：UserAgent 欄位雖存於 audit_log 資料表，此 DTO 刻意省略（冗長且具隱私疑慮）。
/// </summary>
public class AuditLogDto
{
    /// <summary>紀錄唯一識別碼。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid Id { get; set; }

    /// <summary>操作者 user ID；null 表示系統自動動作。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid? Who { get; set; }

    /// <summary>動作類型。</summary>
    /// <example>auth.login.success</example>
    public string Action { get; set; } = "";

    /// <summary>操作對象資源類型。</summary>
    /// <example>User</example>
    public string Target { get; set; } = "";

    /// <summary>操作對象資源 ID。</summary>
    public Guid? TargetId { get; set; }

    /// <summary>動作結果。</summary>
    /// <example>success</example>
    public string Result { get; set; } = "";

    /// <summary>變更前狀態（JSON 字串）。</summary>
    /// <example>null</example>
    public string? Before { get; set; }

    /// <summary>變更後狀態（JSON 字串）。</summary>
    /// <example>null</example>
    public string? After { get; set; }

    /// <summary>操作者 IP 位址。</summary>
    /// <example>1.2.3.4</example>
    public string? Ip { get; set; }

    /// <summary>所屬租戶（creator）ID。</summary>
    public Guid? Tenant { get; set; }

    /// <summary>業務事件實際發生時間（UTC）。</summary>
    public DateTimeOffset OccurredAt { get; set; }

    /// <summary>跨服務追蹤用的 correlation / trace ID。</summary>
    /// <example>00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01</example>
    public string? CorrelationId { get; set; }
}
