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
    public async Task<ActionResult<ListCatalogsResponse>> List([FromQuery] ListCatalogsRequest request, CancellationToken ct) =>
        Ok(await catalogManager.ListAsync(request, publishedOnly: true, ct));

    /// <summary>查詢登入使用者（商店 Owner）的商品列表，含未上架商品。</summary>
    /// <param name="request">查詢條件（須帶 StoreId）。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpGet("mine")]
    [ProducesResponseType<ListCatalogsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ListCatalogsResponse>> ListMine([FromQuery] ListCatalogsRequest request, CancellationToken ct) =>
        Ok(await catalogManager.ListAsync(request, publishedOnly: false, ct));

    /// <summary>查詢指定商店的全部商品（含草稿 / 已下架 / 已停權）。僅 Admin 可操作。</summary>
    /// <param name="storeId">商店 ID。</param>
    /// <param name="request">查詢條件（分類 / 標籤 / 關鍵字 + 分頁）；StoreId 由路徑帶入。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpGet("by-store/{storeId:guid}")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType<ListCatalogsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ListCatalogsResponse>> ListByStore(
        Guid storeId, [FromQuery] ListCatalogsRequest request, CancellationToken ct)
    {
        request.StoreId = storeId;
        return Ok(await catalogManager.ListAsync(request, publishedOnly: false, ct));
    }

    /// <summary>查詢商品完整資訊。未上架商品僅 Owner 可見。</summary>
    /// <param name="id">商品 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType<CatalogDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CatalogDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await catalogManager.GetAsync(id, ct));

    /// <summary>建立商品（草稿）。僅商店 Owner 可操作。</summary>
    /// <param name="request">商品基本資料。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost]
    [ProducesResponseType<CatalogDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CatalogDto>> Create([FromBody] CreateCatalogRequest request, CancellationToken ct)
    {
        var catalog = await catalogManager.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = catalog.Id, version = "1.0" }, catalog);
    }

    /// <summary>更新商品基本資料。僅 Owner 可操作。</summary>
    /// <param name="id">商品 ID。</param>
    /// <param name="request">欲更新的欄位。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType<CatalogDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CatalogDto>> Update(Guid id, [FromBody] UpdateCatalogRequest request, CancellationToken ct) =>
        Ok(await catalogManager.UpdateAsync(id, request, ct));

    /// <summary>設定 / 移除商品分類。僅 Owner 可操作。</summary>
    /// <param name="id">商品 ID。</param>
    /// <param name="request">分類設定。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPut("{id:guid}/category")]
    [ProducesResponseType<CatalogDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CatalogDto>> SetCategory(Guid id, [FromBody] SetCatalogCategoryRequest request, CancellationToken ct) =>
        Ok(await catalogManager.SetCategoryAsync(id, request, ct));

    /// <summary>全量覆蓋商品標籤。僅 Owner 可操作。</summary>
    /// <param name="id">商品 ID。</param>
    /// <param name="request">標籤名稱清單。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPut("{id:guid}/tags")]
    [ProducesResponseType<List<string>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<string>>> SetTags(Guid id, [FromBody] SetCatalogTagsRequest request, CancellationToken ct) =>
        Ok(await catalogManager.SetTagsAsync(id, request, ct));

    /// <summary>上架商品（Draft/Archived → Published）。需已有目前版本。僅 Owner 可操作。</summary>
    /// <param name="id">商品 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Publish(Guid id, CancellationToken ct)
    {
        await catalogManager.PublishAsync(id, ct);
        return NoContent();
    }

    /// <summary>下架封存商品（Published → Archived）。僅 Owner 可操作。</summary>
    /// <param name="id">商品 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost("{id:guid}/archive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Archive(Guid id, CancellationToken ct)
    {
        await catalogManager.ArchiveAsync(id, ct);
        return NoContent();
    }

    /// <summary>刪除商品（軟刪除）。僅未曾上架的草稿可刪除，否則回 409。僅 Owner 可操作。</summary>
    /// <param name="id">商品 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await catalogManager.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>平台停權商品（任意狀態 → Suspended）。僅 Admin 可操作。</summary>
    /// <param name="id">商品 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost("{id:guid}/suspend")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Suspend(Guid id, CancellationToken ct)
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
    public async Task<IActionResult> Unsuspend(Guid id, CancellationToken ct)
    {
        await catalogManager.UnsuspendAsync(id, ct);
        return NoContent();
    }

    /// <summary>商品詳情頁瀏覽次數 +1（公開）。</summary>
    /// <param name="id">商品 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost("{id:guid}/view")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> IncrementView(Guid id, CancellationToken ct)
    {
        await catalogManager.IncrementViewAsync(id, ct);
        return NoContent();
    }

    /// <summary>設為編輯精選（市集首頁精選輪播）。僅 Admin 可操作。</summary>
    /// <param name="id">商品 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost("{id:guid}/feature")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Feature(Guid id, CancellationToken ct)
    {
        await catalogManager.SetFeaturedAsync(id, featured: true, ct);
        return NoContent();
    }

    /// <summary>取消編輯精選。僅 Admin 可操作。</summary>
    /// <param name="id">商品 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost("{id:guid}/unfeature")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Unfeature(Guid id, CancellationToken ct)
    {
        await catalogManager.SetFeaturedAsync(id, featured: false, ct);
        return NoContent();
    }

    /// <summary>設為店長精選（店面首頁 spotlight，接續排在現有精選之後）。僅商店 Owner 可操作。</summary>
    /// <param name="id">商品 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost("{id:guid}/store-feature")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> StoreFeature(Guid id, CancellationToken ct)
    {
        await catalogManager.SetStoreFeaturedAsync(id, featured: true, ct);
        return NoContent();
    }

    /// <summary>取消店長精選。僅商店 Owner 可操作。</summary>
    /// <param name="id">商品 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost("{id:guid}/store-unfeature")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> StoreUnfeature(Guid id, CancellationToken ct)
    {
        await catalogManager.SetStoreFeaturedAsync(id, featured: false, ct);
        return NoContent();
    }

    /// <summary>重排店長精選的顯示順序（全量覆蓋）。僅商店 Owner 可操作。</summary>
    /// <param name="request">商店 ID 與依顯示順序排列的商品 ID 清單。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPut("store-featured/order")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ReorderStoreFeatured([FromBody] ReorderStoreFeaturedRequest request, CancellationToken ct)
    {
        await catalogManager.ReorderStoreFeaturedAsync(request, ct);
        return NoContent();
    }

    /// <summary>申請展示型資產（縮圖 / 截圖 / 預覽影音）上傳簽章 URL。簽發階段不扣配額、不建資產。僅 Owner 可操作。</summary>
    /// <param name="id">商品 ID。</param>
    /// <param name="request">資產資訊。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost("{id:guid}/assets/upload-url")]
    [ProducesResponseType<CatalogAssetUploadUrlResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CatalogAssetUploadUrlResponse>> RequestAssetUploadUrl(
        Guid id, [FromBody] RequestCatalogAssetUploadUrlRequest request, CancellationToken ct) =>
        Ok(await catalogManager.RequestAssetUploadUrlAsync(id, request, ct));

    /// <summary>確認展示型資產上傳完成：扣配額、建立資產並標記檔案已使用。冪等。僅 Owner 可操作。</summary>
    /// <param name="id">商品 ID。</param>
    /// <param name="assetId">資產（檔案）ID。</param>
    /// <param name="request">確認資訊（資產類型）。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost("{id:guid}/assets/{assetId:guid}/confirm")]
    [ProducesResponseType<CatalogAssetDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CatalogAssetDto>> ConfirmAsset(
        Guid id, Guid assetId, [FromBody] ConfirmCatalogAssetRequest request, CancellationToken ct) =>
        Ok(await catalogManager.ConfirmAssetAsync(id, assetId, request, ct));

    /// <summary>刪除展示型資產。僅 Owner 可操作。</summary>
    /// <param name="id">商品 ID。</param>
    /// <param name="assetId">資產 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpDelete("{id:guid}/assets/{assetId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsset(Guid id, Guid assetId, CancellationToken ct)
    {
        await catalogManager.DeleteAssetAsync(id, assetId, ct);
        return NoContent();
    }
}
