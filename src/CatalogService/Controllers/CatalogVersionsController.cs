using Asp.Versioning;
using CatalogService.Models;
using CatalogService.Services;
using CatalogService.Services.CatalogVersions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CatalogService.Controllers;

/// <summary>商品版本 API：版本管理與可下載檔案（買家實際取得的數位內容）上傳 / 下載。版本管理與管理用途下載僅 Owner 可操作；買家下載（<c>GET .../downloads</c>）以購買紀錄授權。</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/catalogs/{catalogId:guid}/versions")]
[Authorize]
public class CatalogVersionsController(ICatalogVersionService versionService) : ControllerBase
{
    /// <summary>列出商品的所有版本（新到舊）。</summary>
    /// <param name="catalogId">商品 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpGet]
    [ProducesResponseType<List<CatalogVersionDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CatalogVersionDto>>> List(Guid catalogId, CancellationToken ct) =>
        Ok(await versionService.ListAsync(catalogId, ct));

    /// <summary>建立新版本，並設為商品的目前版本。</summary>
    /// <param name="catalogId">商品 ID。</param>
    /// <param name="request">版本資訊。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost]
    [ProducesResponseType<CatalogVersionDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CatalogVersionDto>> Create(Guid catalogId, [FromBody] CreateCatalogVersionRequest request, CancellationToken ct)
    {
        var version = await versionService.CreateAsync(catalogId, request, ct);
        return CreatedAtAction(nameof(List), new { catalogId, version = "1.0" }, version);
    }

    /// <summary>申請版本可下載檔案上傳簽章 URL（私有物件）。簽發階段不扣配額、不建資產。</summary>
    /// <param name="catalogId">商品 ID。</param>
    /// <param name="versionId">版本 ID。</param>
    /// <param name="request">檔案資訊。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost("{versionId:guid}/assets/upload-url")]
    [ProducesResponseType<VersionAssetUploadUrlResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<VersionAssetUploadUrlResponse>> RequestAssetUploadUrl(
        Guid catalogId, Guid versionId, [FromBody] RequestVersionAssetUploadUrlRequest request, CancellationToken ct) =>
        Ok(await versionService.RequestAssetUploadUrlAsync(catalogId, versionId, request, ct));

    /// <summary>確認版本可下載檔案上傳完成：扣配額、建立資產並標記檔案已使用。冪等。</summary>
    /// <param name="catalogId">商品 ID。</param>
    /// <param name="versionId">版本 ID。</param>
    /// <param name="assetId">資產（檔案）ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost("{versionId:guid}/assets/{assetId:guid}/confirm")]
    [ProducesResponseType<CatalogVersionAssetDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CatalogVersionAssetDto>> ConfirmAsset(
        Guid catalogId, Guid versionId, Guid assetId, CancellationToken ct) =>
        Ok(await versionService.ConfirmAssetAsync(catalogId, versionId, assetId, ct));

    /// <summary>取得版本可下載檔案的下載簽章 URL（管理用途）。</summary>
    /// <param name="catalogId">商品 ID。</param>
    /// <param name="versionId">版本 ID。</param>
    /// <param name="assetId">檔案 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpGet("{versionId:guid}/assets/{assetId:guid}/download-url")]
    [ProducesResponseType<StorageDownloadUrlResult>(StatusCodes.Status200OK)]
    public async Task<ActionResult<StorageDownloadUrlResult>> GetAssetDownloadUrl(
        Guid catalogId, Guid versionId, Guid assetId, CancellationToken ct) =>
        Ok(await versionService.GetAssetDownloadUrlAsync(catalogId, versionId, assetId, ct));

    /// <summary>列出買家已購商品某版本的可下載檔案（含短效下載 URL）。登入買家以購買紀錄授權；訪客憑訂單 ID（隨訂單完成信寄出的下載憑證）授權。</summary>
    /// <param name="catalogId">商品 ID。</param>
    /// <param name="versionId">版本 ID。</param>
    /// <param name="orderId">訪客下載憑證：已完成且包含此商品的訂單 ID；登入買家可省略。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpGet("{versionId:guid}/downloads")]
    [AllowAnonymous]
    [ProducesResponseType<List<PurchasedVersionAssetDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PurchasedVersionAssetDto>>> ListPurchasedDownloads(
        Guid catalogId, Guid versionId, [FromQuery] Guid? orderId, CancellationToken ct) =>
        Ok(await versionService.ListPurchasedDownloadsAsync(catalogId, versionId, orderId, ct));

    /// <summary>刪除版本可下載檔案。</summary>
    /// <param name="catalogId">商品 ID。</param>
    /// <param name="versionId">版本 ID。</param>
    /// <param name="assetId">檔案 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpDelete("{versionId:guid}/assets/{assetId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsset(Guid catalogId, Guid versionId, Guid assetId, CancellationToken ct)
    {
        await versionService.DeleteAssetAsync(catalogId, versionId, assetId, ct);
        return NoContent();
    }
}
