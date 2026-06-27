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
    public async Task<ActionResult<List<CatalogVersionDto>>> ListAsync(Guid catalogId, CancellationToken ct) =>
        Ok(await versionService.ListAsync(catalogId, ct));

    /// <summary>建立新版本，並設為商品的目前版本。</summary>
    /// <param name="catalogId">商品 ID。</param>
    /// <param name="request">版本資訊。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost]
    [ProducesResponseType<CatalogVersionDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CatalogVersionDto>> CreateAsync(Guid catalogId, [FromBody] CreateCatalogVersionRequest request, CancellationToken ct)
    {
        var version = await versionService.CreateAsync(catalogId, request, ct);
        return CreatedAtAction(nameof(ListAsync), new { catalogId, version = "1.0" }, version);
    }

    /// <summary>申請版本可下載檔案上傳簽章 URL（私有物件）。</summary>
    /// <param name="catalogId">商品 ID。</param>
    /// <param name="versionId">版本 ID。</param>
    /// <param name="request">檔案資訊。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost("{versionId:guid}/assets/upload-url")]
    [ProducesResponseType<VersionAssetUploadUrlResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<VersionAssetUploadUrlResponse>> RequestAssetUploadUrlAsync(
        Guid catalogId, Guid versionId, [FromBody] RequestVersionAssetUploadUrlRequest request, CancellationToken ct) =>
        Ok(await versionService.RequestAssetUploadUrlAsync(catalogId, versionId, request, ct));

    /// <summary>取得版本可下載檔案的下載簽章 URL（管理用途）。</summary>
    /// <param name="catalogId">商品 ID。</param>
    /// <param name="versionId">版本 ID。</param>
    /// <param name="assetId">檔案 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpGet("{versionId:guid}/assets/{assetId:guid}/download-url")]
    [ProducesResponseType<StorageDownloadUrlResult>(StatusCodes.Status200OK)]
    public async Task<ActionResult<StorageDownloadUrlResult>> GetAssetDownloadUrlAsync(
        Guid catalogId, Guid versionId, Guid assetId, CancellationToken ct) =>
        Ok(await versionService.GetAssetDownloadUrlAsync(catalogId, versionId, assetId, ct));

    /// <summary>列出買家已購商品某版本的可下載檔案（含短效下載 URL）。以購買紀錄授權，須已有該商品的完成訂單。</summary>
    /// <param name="catalogId">商品 ID。</param>
    /// <param name="versionId">版本 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpGet("{versionId:guid}/downloads")]
    [ProducesResponseType<List<PurchasedVersionAssetDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PurchasedVersionAssetDto>>> ListPurchasedDownloadsAsync(
        Guid catalogId, Guid versionId, CancellationToken ct) =>
        Ok(await versionService.ListPurchasedDownloadsAsync(catalogId, versionId, ct));

    /// <summary>刪除版本可下載檔案。</summary>
    /// <param name="catalogId">商品 ID。</param>
    /// <param name="versionId">版本 ID。</param>
    /// <param name="assetId">檔案 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpDelete("{versionId:guid}/assets/{assetId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAssetAsync(Guid catalogId, Guid versionId, Guid assetId, CancellationToken ct)
    {
        await versionService.DeleteAssetAsync(catalogId, versionId, assetId, ct);
        return NoContent();
    }
}
