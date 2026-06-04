namespace LogService.Models;

/// <summary>稽核事件查詢請求（分頁採 offset / limit）。</summary>
public class GetAuditLogsRequest
{
    /// <summary>略過筆數。</summary>
    /// <example>0</example>
    public int Offset { get; set; } = 0;

    /// <summary>每頁筆數（最大 100）。</summary>
    /// <example>20</example>
    public int Limit { get; set; } = 20;

    /// <summary>過濾操作者 user ID。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid? Who { get; set; }

    /// <summary>過濾動作類型（精確比對）。</summary>
    /// <example>auth.login.success</example>
    public string? Action { get; set; }

    /// <summary>過濾操作對象資源類型。</summary>
    /// <example>User</example>
    public string? Target { get; set; }

    /// <summary>過濾所屬租戶 ID。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid? Tenant { get; set; }

    /// <summary>查詢起始時間（UTC，含）。</summary>
    /// <example>2026-01-01T00:00:00Z</example>
    public DateTimeOffset? From { get; set; }

    /// <summary>查詢結束時間（UTC，含）。</summary>
    /// <example>2026-12-31T23:59:59Z</example>
    public DateTimeOffset? To { get; set; }
}
