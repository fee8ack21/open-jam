using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuotaService.Models;
using QuotaService.Services.Quotas;
using Shared.Auth;
using Shared.Exceptions;

namespace QuotaService.Controllers;

/// <summary>租戶用量查詢 API（後台顯示）。</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/usage")]
[Authorize]
public class UsageController(IQuotaManager quota, ICurrentUserAccessor currentUser) : ControllerBase
{
    /// <summary>查詢目前登入租戶的用量。</summary>
    /// <param name="ct">Cancellation token。</param>
    [HttpGet("me")]
    [ProducesResponseType<UsageResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UsageResponse>> GetMineAsync(CancellationToken ct)
    {
        var tenantId = currentUser.UserId ?? throw new UnauthorizedException();
        return Ok(await quota.GetUsageAsync(tenantId, ct));
    }

    /// <summary>查詢指定租戶的用量（管理員）。</summary>
    /// <param name="tenantId">租戶（創作者）ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpGet("{tenantId:guid}")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType<UsageResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UsageResponse>> GetAsync(Guid tenantId, CancellationToken ct) =>
        Ok(await quota.GetUsageAsync(tenantId, ct));
}
