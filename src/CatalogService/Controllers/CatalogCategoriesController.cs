using Asp.Versioning;
using CatalogService.Models;
using CatalogService.Services.Categories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CatalogService.Controllers;

/// <summary>商品分類 API：瀏覽（公開）與維護（Admin）。以 ParentId 實踐多層子分類。</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/catalog-categories")]
public class CatalogCategoriesController(ICatalogCategoryService categoryService) : ControllerBase
{
    /// <summary>列出所有分類（公開）。</summary>
    /// <param name="ct">Cancellation token。</param>
    [HttpGet]
    [ProducesResponseType<List<CatalogCategoryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CatalogCategoryDto>>> List(CancellationToken ct) =>
        Ok(await categoryService.ListAsync(ct));

    /// <summary>查詢單一分類（公開）。</summary>
    /// <param name="id">分類 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<CatalogCategoryDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CatalogCategoryDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await categoryService.GetAsync(id, ct));

    /// <summary>建立分類。僅 Admin 可操作。</summary>
    /// <param name="request">分類資料。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType<CatalogCategoryDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CatalogCategoryDto>> Create([FromBody] CreateCatalogCategoryRequest request, CancellationToken ct)
    {
        var category = await categoryService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = category.Id, version = "1.0" }, category);
    }

    /// <summary>更新分類。僅 Admin 可操作。</summary>
    /// <param name="id">分類 ID。</param>
    /// <param name="request">欲更新的欄位。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPatch("{id:guid}")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType<CatalogCategoryDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CatalogCategoryDto>> Update(Guid id, [FromBody] UpdateCatalogCategoryRequest request, CancellationToken ct) =>
        Ok(await categoryService.UpdateAsync(id, request, ct));

    /// <summary>刪除分類（不可有子分類或被商品引用）。僅 Admin 可操作。</summary>
    /// <param name="id">分類 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await categoryService.DeleteAsync(id, ct);
        return NoContent();
    }
}
