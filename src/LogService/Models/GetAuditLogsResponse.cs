namespace LogService.Models;

/// <summary>稽核事件分頁查詢回應。</summary>
public class GetAuditLogsResponse
{
    /// <summary>符合條件的總筆數（未分頁）。</summary>
    /// <example>142</example>
    public int TotalCount { get; set; }

    /// <summary>本頁稽核事件清單。</summary>
    public List<AuditLogDto> Items { get; set; } = [];
}
