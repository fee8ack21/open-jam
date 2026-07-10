using Asp.Versioning;
using ContentService.Models;
using ContentService.Services.FaqCategories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContentService.Controllers;

/// <summary>
/// 常見問題主題分類 API：瀏覽（匿名公開）與維護（Admin）。
/// portal-web FAQ 頁以公開列表建立主題分頁；workspace-web 管理員後台維護分類。
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/faq-categories")]
public class FaqCategoriesController(IFaqCategoryService categoryService) : ControllerBase
{
    /// <summary>列出所有分類（匿名公開，依排序）。</summary>
    /// <param name="ct">Cancellation token。</param>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<List<FaqCategoryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<FaqCategoryDto>>> ListAsync(CancellationToken ct) =>
        Ok(await categoryService.ListAsync(ct));

    /// <summary>查詢單一分類。僅 Admin 可存取。</summary>
    /// <param name="id">分類 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType<FaqCategoryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FaqCategoryDto>> GetAsync(Guid id, CancellationToken ct) =>
        Ok(await categoryService.GetAsync(id, ct));

    /// <summary>建立分類。僅 Admin 可操作。</summary>
    /// <param name="request">分類資料。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType<FaqCategoryDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<FaqCategoryDto>> CreateAsync([FromBody] CreateFaqCategoryRequest request, CancellationToken ct)
    {
        var category = await categoryService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetAsync), new { id = category.Id, version = "1.0" }, category);
    }

    /// <summary>更新分類。僅 Admin 可操作。</summary>
    /// <param name="id">分類 ID。</param>
    /// <param name="request">欲更新的欄位。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPatch("{id:guid}")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType<FaqCategoryDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<FaqCategoryDto>> UpdateAsync(Guid id, [FromBody] UpdateFaqCategoryRequest request, CancellationToken ct) =>
        Ok(await categoryService.UpdateAsync(id, request, ct));

    /// <summary>刪除分類（不可仍被常見問題項目引用）。僅 Admin 可操作。</summary>
    /// <param name="id">分類 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct)
    {
        await categoryService.DeleteAsync(id, ct);
        return NoContent();
    }
}
