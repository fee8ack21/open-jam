using Asp.Versioning;
using CatalogService.Models;
using CatalogService.Services.Catalogs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CatalogService.Controllers;

/// <summary>商品 API：建立、查詢、更新、狀態管理（上架／下架／停權）、分類 / 標籤、展示型資產上傳。</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/catalogs")]
[Authorize]
public class CatalogsController(ICatalogManager catalogManager) : ControllerBase
{
    /// <summary>瀏覽已上架商品列表（公開）。</summary>
    /// <param name="request">查詢條件（商店 / 分類 / 標籤 / 關鍵字 + 分頁）。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<ListCatalogsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ListCatalogsResponse>> ListAsync([FromQuery] ListCatalogsRequest request, CancellationToken ct) =>
        Ok(await catalogManager.ListAsync(request, publishedOnly: true, ct));

    /// <summary>查詢登入使用者（商店 Owner）的商品列表，含未上架商品。</summary>
    /// <param name="request">查詢條件（須帶 StoreId）。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpGet("mine")]
    [ProducesResponseType<ListCatalogsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ListCatalogsResponse>> ListMineAsync([FromQuery] ListCatalogsRequest request, CancellationToken ct) =>
        Ok(await catalogManager.ListAsync(request, publishedOnly: false, ct));

    /// <summary>查詢商品完整資訊。未上架商品僅 Owner 可見。</summary>
    /// <param name="id">商品 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType<CatalogDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CatalogDto>> GetAsync(Guid id, CancellationToken ct) =>
        Ok(await catalogManager.GetAsync(id, ct));

    /// <summary>建立商品（草稿）。僅商店 Owner 可操作。</summary>
    /// <param name="request">商品基本資料。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost]
    [ProducesResponseType<CatalogDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CatalogDto>> CreateAsync([FromBody] CreateCatalogRequest request, CancellationToken ct)
    {
        var catalog = await catalogManager.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetAsync), new { id = catalog.Id, version = "1.0" }, catalog);
    }

    /// <summary>更新商品基本資料。僅 Owner 可操作。</summary>
    /// <param name="id">商品 ID。</param>
    /// <param name="request">欲更新的欄位。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType<CatalogDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CatalogDto>> UpdateAsync(Guid id, [FromBody] UpdateCatalogRequest request, CancellationToken ct) =>
        Ok(await catalogManager.UpdateAsync(id, request, ct));

    /// <summary>設定 / 移除商品分類。僅 Owner 可操作。</summary>
    /// <param name="id">商品 ID。</param>
    /// <param name="request">分類設定。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPut("{id:guid}/category")]
    [ProducesResponseType<CatalogDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CatalogDto>> SetCategoryAsync(Guid id, [FromBody] SetCatalogCategoryRequest request, CancellationToken ct) =>
        Ok(await catalogManager.SetCategoryAsync(id, request, ct));

    /// <summary>全量覆蓋商品標籤。僅 Owner 可操作。</summary>
    /// <param name="id">商品 ID。</param>
    /// <param name="request">標籤名稱清單。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPut("{id:guid}/tags")]
    [ProducesResponseType<List<string>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<string>>> SetTagsAsync(Guid id, [FromBody] SetCatalogTagsRequest request, CancellationToken ct) =>
        Ok(await catalogManager.SetTagsAsync(id, request, ct));

    /// <summary>上架商品（Draft/Archived → Published）。需已有目前版本。僅 Owner 可操作。</summary>
    /// <param name="id">商品 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> PublishAsync(Guid id, CancellationToken ct)
    {
        await catalogManager.PublishAsync(id, ct);
        return NoContent();
    }

    /// <summary>下架封存商品（Published → Archived）。僅 Owner 可操作。</summary>
    /// <param name="id">商品 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost("{id:guid}/archive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ArchiveAsync(Guid id, CancellationToken ct)
    {
        await catalogManager.ArchiveAsync(id, ct);
        return NoContent();
    }

    /// <summary>平台停權商品（任意狀態 → Suspended）。僅 Admin 可操作。</summary>
    /// <param name="id">商品 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost("{id:guid}/suspend")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SuspendAsync(Guid id, CancellationToken ct)
    {
        await catalogManager.SuspendAsync(id, ct);
        return NoContent();
    }

    /// <summary>解除商品停權（Suspended → Archived）。僅 Admin 可操作。</summary>
    /// <param name="id">商品 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost("{id:guid}/unsuspend")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UnsuspendAsync(Guid id, CancellationToken ct)
    {
        await catalogManager.UnsuspendAsync(id, ct);
        return NoContent();
    }

    /// <summary>申請展示型資產（縮圖 / 截圖 / 預覽影音）上傳簽章 URL。僅 Owner 可操作。</summary>
    /// <param name="id">商品 ID。</param>
    /// <param name="request">資產資訊。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost("{id:guid}/assets/upload-url")]
    [ProducesResponseType<CatalogAssetUploadUrlResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CatalogAssetUploadUrlResponse>> RequestAssetUploadUrlAsync(
        Guid id, [FromBody] RequestCatalogAssetUploadUrlRequest request, CancellationToken ct) =>
        Ok(await catalogManager.RequestAssetUploadUrlAsync(id, request, ct));

    /// <summary>刪除展示型資產。僅 Owner 可操作。</summary>
    /// <param name="id">商品 ID。</param>
    /// <param name="assetId">資產 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpDelete("{id:guid}/assets/{assetId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAssetAsync(Guid id, Guid assetId, CancellationToken ct)
    {
        await catalogManager.DeleteAssetAsync(id, assetId, ct);
        return NoContent();
    }
}
