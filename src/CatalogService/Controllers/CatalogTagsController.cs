using Asp.Versioning;
using CatalogService.Models;
using CatalogService.Services.Tags;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CatalogService.Controllers;

/// <summary>商品標籤 API：瀏覽 / 搜尋（公開）。標籤於商品掛載標籤時自動建立。</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/catalog-tags")]
public class CatalogTagsController(ICatalogTagService tagService) : ControllerBase
{
    /// <summary>分頁查詢標籤（依使用次數遞減，公開）。</summary>
    /// <param name="request">查詢條件（前綴搜尋 + 分頁）。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpGet]
    [ProducesResponseType<ListCatalogTagsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ListCatalogTagsResponse>> ListAsync([FromQuery] ListCatalogTagsRequest request, CancellationToken ct) =>
        Ok(await tagService.ListAsync(request, ct));
}
