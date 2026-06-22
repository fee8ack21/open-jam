using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuotaService.Models;
using QuotaService.Services.Quotas;

namespace QuotaService.Controllers;

/// <summary>儲存空間預扣 API：功能 API 於簽發上傳 signed URL 前後呼叫。租戶取自 JWT sub。</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/reservations")]
[Authorize]
public class ReservationsController(IQuotaManager quota) : ControllerBase
{
    /// <summary>建立預扣（含單檔 / 單商品即時上限檢查）。冪等：同 ReservationId 回傳既有結果。</summary>
    /// <param name="request">預扣請求。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost]
    [ProducesResponseType<ReservationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ReservationResponse>> ReserveAsync(
        ReserveRequest request, CancellationToken ct) =>
        Ok(await quota.ReserveAsync(request, ct));

    /// <summary>釋放預扣（取消 / 失敗 / 主動釋放）。</summary>
    /// <param name="id">預扣紀錄 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost("{id:guid}/release")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReleaseAsync(Guid id, CancellationToken ct)
    {
        await quota.ReleaseAsync(id, ct);
        return NoContent();
    }
}
