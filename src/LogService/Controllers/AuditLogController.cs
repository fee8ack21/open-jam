using LogService.Data;
using LogService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogService.Controllers;

/// <summary>
/// 稽核日誌查詢 API。
/// 查詢權限說明（MVP 暫無 JWT 驗證，待 Auth 服務整合後補上）：
///   - 管理員：可查全平台紀錄（不帶 who / tenant 篩選）
///   - 用戶：應限制只查自己的 who = currentUserId
///   - 創作者：應限制只查自己的 tenant = currentTenantId
/// </summary>
[ApiController]
[Route("audit-logs")]
public class AuditLogController(LogDbContext db) : ControllerBase
{
    /// <summary>查詢稽核事件（分頁，支援多條件篩選）。</summary>
    /// <param name="request">篩選與分頁參數。</param>
    /// <param name="ct">Cancellation token。</param>
    /// <returns>符合條件的稽核事件分頁結果。</returns>
    [HttpGet]
    [ProducesResponseType<GetAuditLogsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<GetAuditLogsResponse>> GetAsync([FromQuery] GetAuditLogsRequest request, CancellationToken ct)
    {
        var limit = Math.Clamp(request.Limit, 1, 100);

        var query = db.AuditLogs.AsNoTracking();

        if (request.Who.HasValue)
            query = query.Where(a => a.Who == request.Who);

        if (!string.IsNullOrWhiteSpace(request.Action))
            query = query.Where(a => a.Action == request.Action);

        if (!string.IsNullOrWhiteSpace(request.Target))
            query = query.Where(a => a.Target == request.Target);

        if (request.Tenant.HasValue)
            query = query.Where(a => a.Tenant == request.Tenant);

        if (request.From.HasValue)
            query = query.Where(a => a.OccurredAt >= request.From);

        if (request.To.HasValue)
            query = query.Where(a => a.OccurredAt <= request.To);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(a => a.OccurredAt)
            .Skip(request.Offset)
            .Take(limit)
            .Select(a => new AuditLogDto
            {
                Id            = a.Id,
                Who           = a.Who,
                Action        = a.Action,
                Target        = a.Target,
                TargetId      = a.TargetId,
                Result        = a.Result,
                Before        = a.Before,
                After         = a.After,
                Ip            = a.Ip,
                Tenant        = a.Tenant,
                OccurredAt    = a.OccurredAt,
                CorrelationId = a.CorrelationId,
            })
            .ToListAsync(ct);

        return Ok(new GetAuditLogsResponse { TotalCount = total, Items = items });
    }
}
