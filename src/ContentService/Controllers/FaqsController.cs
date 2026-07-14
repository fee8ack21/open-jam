using Asp.Versioning;
using ContentService.Models;
using ContentService.Services.Faqs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContentService.Controllers;

/// <summary>
/// 常見問題（FAQ）管理 REST API。
/// 管理端點僅具 "Admin" 角色的 Hydra access token 可存取；
/// 「已發布項目」查詢為匿名公開（portal-web FAQ 頁呈現用）。
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/faqs")]
[Authorize(Roles = "Admin")]
public class FaqsController(IFaqService faqService) : ControllerBase
{
    /// <summary>查詢常見問題（分頁，支援分類 / 發布狀態篩選）。</summary>
    /// <param name="request">篩選與分頁參數。</param>
    /// <param name="ct">Cancellation token。</param>
    /// <returns>符合條件的項目分頁結果。</returns>
    [HttpGet]
    [ProducesResponseType<ListFaqItemsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ListFaqItemsResponse>> List([FromQuery] ListFaqItemsRequest request, CancellationToken ct)
        => Ok(await faqService.ListAsync(request, ct));

    /// <summary>取得已發布的常見問題（匿名公開，依分類與排序）。</summary>
    /// <param name="categoryId">主題分類 ID；不帶時回傳所有分類。</param>
    /// <param name="ct">Cancellation token。</param>
    /// <returns>已發布的項目清單。</returns>
    [HttpGet("published")]
    [AllowAnonymous]
    [ProducesResponseType<List<FaqItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<FaqItemDto>>> GetPublished([FromQuery] Guid? categoryId, CancellationToken ct)
        => Ok(await faqService.GetPublishedAsync(categoryId, ct));

    /// <summary>取得單筆常見問題。</summary>
    /// <param name="id">項目 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    /// <returns>項目內容。</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<FaqItemDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FaqItemDto>> Get(Guid id, CancellationToken ct)
        => Ok(await faqService.GetAsync(id, ct));

    /// <summary>建立常見問題項目。</summary>
    /// <param name="request">分類、問題、解答、排序與發布狀態。</param>
    /// <param name="ct">Cancellation token。</param>
    /// <returns>建立完成的項目。</returns>
    [HttpPost]
    [ProducesResponseType<FaqItemDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<FaqItemDto>> Create([FromBody] CreateFaqItemRequest request, CancellationToken ct)
    {
        var dto = await faqService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = dto.Id, version = "1" }, dto);
    }

    /// <summary>更新常見問題項目。</summary>
    /// <param name="id">項目 ID。</param>
    /// <param name="request">更新後的內容。</param>
    /// <param name="ct">Cancellation token。</param>
    /// <returns>更新後的項目。</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<FaqItemDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FaqItemDto>> Update(Guid id, [FromBody] UpdateFaqItemRequest request, CancellationToken ct)
        => Ok(await faqService.UpdateAsync(id, request, ct));

    /// <summary>刪除常見問題項目。</summary>
    /// <param name="id">項目 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await faqService.DeleteAsync(id, ct);
        return NoContent();
    }
}
