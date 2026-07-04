using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StorageService.Models;
using StorageService.Services.Files;

namespace StorageService.Controllers;

/// <summary>
/// 數位商品檔案管理 API。
/// 調用方為功能 API（商品服務），前端不直接呼叫此 Controller，
/// 而是透過簽章 URL 直接與儲存後端（本地 blob 端點 / GCS）互動。
///
/// 授權說明（MVP 暫無 JWT 驗證，待功能 API 整合後補上 service token 驗證）。
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/files")]
public class FilesController(IFileService fileService) : ControllerBase
{
    /// <summary>申請上傳簽章 URL（presigned PUT）。</summary>
    /// <remarks>
    /// 簽發階段不涉及配額：配額於使用者提交確認、功能 API 建立 reference 時才計量。
    /// 本地儲存於 blob 端點接收上傳後即觸發處理 pipeline；GCS 由 bucket notification 觸發，
    /// 皆無需客戶端另行回報。
    /// </remarks>
    /// <param name="request">上傳元資訊。</param>
    /// <param name="ct">Cancellation token。</param>
    /// <returns>包含 presigned URL 與 fileId 的回應。</returns>
    [HttpPost("upload-url")]
    [ProducesResponseType<RequestUploadUrlResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<RequestUploadUrlResponse>> RequestUploadUrlAsync(
        [FromBody] RequestUploadUrlRequest request, CancellationToken ct) =>
        Ok(await fileService.RequestUploadUrlAsync(request, ct));

    /// <summary>加總指定創作者已 Ready 且已被使用（referenced）檔案的位元組總和（QuotaService 每日對帳用）。</summary>
    /// <param name="creatorId">創作者（租戶）ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpGet("usage")]
    [ProducesResponseType<TenantUsageResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TenantUsageResponse>> GetTenantUsageAsync(
        [FromQuery] Guid creatorId, CancellationToken ct) =>
        Ok(await fileService.GetTenantUsageAsync(creatorId, ct));

    /// <summary>彙總全平台儲存用量（數量 / 大小 / 公開私有 / 孤兒檔 / 創作者明細）。僅 Admin 可操作。</summary>
    /// <param name="ct">Cancellation token。</param>
    [HttpGet("usage/summary")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType<PlatformUsageResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PlatformUsageResponse>> GetPlatformUsageAsync(CancellationToken ct) =>
        Ok(await fileService.GetPlatformUsageAsync(ct));

    /// <summary>確認檔案已直傳完成，觸發處理 pipeline 並標記 Ready。</summary>
    /// <remarks>
    /// 雲端（GCS）模式下，前端透過簽章 URL 直傳後由功能 API 呼叫此端點確認；
    /// StorageService 驗證物件確實存在後執行處理並發布 FileReadyEvent。
    /// 本地儲存由 blob 端點接收上傳時自動觸發，無需呼叫。冪等。
    /// </remarks>
    /// <param name="id">檔案 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost("{id:guid}/confirm")]
    [ProducesResponseType<FileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<FileDto>> ConfirmUploadAsync(Guid id, CancellationToken ct) =>
        Ok(await fileService.ConfirmUploadAsync(id, ct));

    /// <summary>標記檔案已被實際使用（建立 File reference）。</summary>
    /// <remarks>
    /// 功能 API 在使用者提交確認、完成配額計量並建立資產 reference 後呼叫。
    /// 僅 Ready 檔案可標記；未標記的檔案不計入配額，且逾期未被使用將由清理排程回收。冪等。
    /// </remarks>
    /// <param name="id">檔案 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPost("{id:guid}/reference")]
    [ProducesResponseType<FileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<FileDto>> MarkReferencedAsync(Guid id, CancellationToken ct) =>
        Ok(await fileService.MarkReferencedAsync(id, ct));

    /// <summary>查詢檔案元資訊與處理狀態。</summary>
    /// <param name="id">檔案 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<FileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FileDto>> GetAsync(Guid id, CancellationToken ct) =>
        Ok(await fileService.GetAsync(id, ct));

    /// <summary>取得已授權的下載簽章 URL（presigned GET）。</summary>
    /// <remarks>
    /// 此端點不自行驗證買家是否擁有商品（entitlement check），
    /// 應由功能 API 完成授權驗證後再呼叫此端點。
    /// 僅 Ready 狀態的檔案才能取得下載 URL。
    /// </remarks>
    /// <param name="id">檔案 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpGet("{id:guid}/download-url")]
    [ProducesResponseType<GetDownloadUrlResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<GetDownloadUrlResponse>> GetDownloadUrlAsync(Guid id, CancellationToken ct) =>
        Ok(await fileService.GetDownloadUrlAsync(id, ct));

    /// <summary>軟刪除檔案；已購買的商品仍保留買家下載權。</summary>
    /// <param name="id">檔案 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct)
    {
        await fileService.DeleteAsync(id, ct);
        return NoContent();
    }
}
