using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuotaService.Models;
using QuotaService.Services.Quotas;

namespace QuotaService.Controllers;

/// <summary>儲存空間扣量 API：使用者提交確認、功能 API 建立檔案 reference 時呼叫。租戶取自 JWT sub。</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/charges")]
[Authorize]
public class ChargesController(IQuotaManager quota) : ControllerBase
{
    /// <summary>扣量（含單檔 / 單商品 / 帳號總量原子檢查）。冪等：同 ChargeId 重送不重複扣量。</summary>
    /// <param name="request">扣量請求。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ChargeAsync(ChargeRequest request, CancellationToken ct)
    {
        await quota.ChargeAsync(request, ct);
        return NoContent();
    }
}
