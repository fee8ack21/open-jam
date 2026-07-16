using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using StorageService.Services.StorageEvents;
using StorageService.Storage;

namespace StorageService.Controllers;

/// <summary>
/// 地端本地儲存的檔案直傳 / 下載端點（取代雲端 presigned URL 的角色）。
/// 客戶端使用 <see cref="FilesController"/> 簽發的 blob URL（帶 <c>expires</c> + <c>sig</c>）
/// 直接 PUT 上傳 / GET 下載，本端點以 <see cref="BlobUrlSigner"/> 驗章授權。
/// 僅在 <c>Storage:Provider=Local</c> 時使用；雲端 GCS 由前端直連 GCS。
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/files/blob")]
public class BlobController(
    LocalFileStore fileStore,
    BlobUrlSigner signer,
    IStorageEventService storageEvents) : ControllerBase
{
    /// <summary>接收客戶端直傳的檔案內容，寫入本地儲存後觸發處理 pipeline。</summary>
    /// <param name="key">物件鍵值（catch-all 路徑）。</param>
    /// <param name="expires">URL 到期時間（Unix 秒）。</param>
    /// <param name="sig">HMAC 簽章。</param>
    /// <param name="max">上傳大小上限（bytes）；由簽發端綁入簽章，接收時強制。</param>
    /// <param name="ct">Cancellation token。</param>
    [HttpPut("{**key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    public async Task<IActionResult> UploadAsync(
        string key, [FromQuery] long expires, [FromQuery] string? sig, [FromQuery] long max, CancellationToken ct)
    {
        // max 一併驗章：竄改 max 會使簽章驗證失敗，無法放寬上限。
        if (!signer.Verify("PUT", key, expires, sig, max))
            return Forbid();

        // 有 Content-Length 且已超過上限者，直接拒絕、不落地。
        if (max > 0 && Request.ContentLength is > 0 && Request.ContentLength > max)
            return StatusCode(StatusCodes.Status413PayloadTooLarge);

        long size;
        try
        {
            // 接收時強制上限（涵蓋未帶 / 謊報 Content-Length 的情形）；超過即中止並清掉部分寫入。
            size = await fileStore.SaveAsync(key, Request.Body, ct, max);
        }
        catch (InvalidOperationException)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge);
        }

        // 本地直傳完成後，直接觸發與 webhook 等價的處理流程（掃毒 / 轉碼 / 標記 Ready）。
        // 公開物件的 blob 路徑帶 public/ 虛擬 bucket 段（對應雲端公開 bucket），
        // 剝除後才是 StorageKey（與 GCS bucket notification 給的物件名一致）。
        var objectKey = key.StartsWith("public/", StringComparison.Ordinal)
            ? key["public/".Length..]
            : key;
        await storageEvents.HandleObjectCreatedAsync(objectKey, size, ct);

        return Ok();
    }

    /// <summary>提供本地儲存檔案的下載；<c>public/</c> 虛擬 bucket 路徑段免簽章，其餘須帶有效簽章。</summary>
    /// <param name="key">物件鍵值（catch-all 路徑）。</param>
    /// <param name="expires">URL 到期時間（Unix 秒）；公開物件可省略。</param>
    /// <param name="sig">HMAC 簽章；公開物件可省略。</param>
    [HttpGet("{**key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Download(string key, [FromQuery] long expires, [FromQuery] string? sig)
    {
        var isPublic = key.StartsWith("public/", StringComparison.Ordinal);
        if (!isPublic && !signer.Verify("GET", key, expires, sig))
            return Forbid();

        if (!fileStore.Exists(key))
            return NotFound();

        var stream = fileStore.OpenRead(key);
        return File(stream, "application/octet-stream", enableRangeProcessing: true);
    }
}
