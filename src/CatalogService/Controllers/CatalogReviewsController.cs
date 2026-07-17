using Asp.Versioning;
using CatalogService.Models;
using CatalogService.Services.Reviews;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CatalogService.Controllers;

/// <summary>商品評論 API：列表（公開）、撰寫 / 查詢 / 刪除本人評論（須登入且已購買）。</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/catalogs/{catalogId:guid}/reviews")]
[Authorize]
public class CatalogReviewsController(ICatalogReviewService reviewService) : ControllerBase
{
    /// <summary>分頁列出商品評論（公開），含平均分與評論數。</summary>
    /// <param name="catalogId">商品 ID。</param>
    /// <param name="request">分頁條件。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<ListReviewsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ListReviewsResponse>> List(
        Guid catalogId, [FromQuery] ListReviewsRequest request, CancellationToken ct) =>
        Ok(await reviewService.ListAsync(catalogId, request, ct));

    /// <summary>
    /// 取得評論者對此商品的評論；尚未評論回傳 204。
    /// 登入買家由 JWT 帶入身分；未註冊訪客傳 <paramref name="orderId"/>（下單憑證）以下單信箱識別。
    /// </summary>
    /// <param name="catalogId">商品 ID。</param>
    /// <param name="orderId">訪客評論憑證：已完成且含此商品的訂單 ID；登入者免帶。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpGet("mine")]
    [AllowAnonymous]
    [ProducesResponseType<CatalogReviewDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<CatalogReviewDto>> GetMine(
        Guid catalogId, [FromQuery] Guid? orderId, CancellationToken ct)
    {
        var review = await reviewService.GetMineAsync(catalogId, orderId, ct);
        return review is null ? NoContent() : Ok(review);
    }

    /// <summary>
    /// 新增 / 更新本人對此商品的評論（一人一則）。須為已購買者：
    /// 登入買家由 JWT 帶入身分；未註冊訪客傳 <paramref name="orderId"/> 以下單信箱識別。
    /// </summary>
    /// <param name="catalogId">商品 ID。</param>
    /// <param name="request">評分與留言。</param>
    /// <param name="orderId">訪客評論憑證：已完成且含此商品的訂單 ID；登入者免帶。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPut("mine")]
    [AllowAnonymous]
    [ProducesResponseType<CatalogReviewDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CatalogReviewDto>> UpsertMine(
        Guid catalogId, [FromBody] UpsertReviewRequest request, [FromQuery] Guid? orderId, CancellationToken ct) =>
        Ok(await reviewService.UpsertAsync(catalogId, request, orderId, ct));

    /// <summary>刪除本人對此商品的評論。訪客傳 <paramref name="orderId"/> 識別身分。</summary>
    /// <param name="catalogId">商品 ID。</param>
    /// <param name="orderId">訪客評論憑證：已完成且含此商品的訂單 ID；登入者免帶。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpDelete("mine")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteMine(Guid catalogId, [FromQuery] Guid? orderId, CancellationToken ct)
    {
        await reviewService.DeleteMineAsync(catalogId, orderId, ct);
        return NoContent();
    }
}
