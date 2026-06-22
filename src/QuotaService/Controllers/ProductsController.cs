using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuotaService.Models;
using QuotaService.Services.Quotas;

namespace QuotaService.Controllers;

/// <summary>上架商品數計數 API：CatalogService 於商品進出 Published 狀態時呼叫。租戶取自 JWT sub。</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/products")]
[Authorize]
public class ProductsController(IQuotaManager quota) : ControllerBase
{
    /// <summary>增減上架商品數（+1 進入 Published、-1 離開 Published），原子檢查上限。</summary>
    /// <param name="request">增減請求。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost("count")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ChangeCountAsync(
        ChangeProductCountRequest request, CancellationToken ct)
    {
        await quota.ChangeProductCountAsync(request.Delta, ct);
        return NoContent();
    }
}
