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
    /// <param name="ct">Cancellation token。</param>
    [HttpPut("{**key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UploadAsync(
        string key, [FromQuery] long expires, [FromQuery] string? sig, CancellationToken ct)
    {
        if (!signer.Verify("PUT", key, expires, sig))
            return Forbid();

        var size = await fileStore.SaveAsync(key, Request.Body, ct);

        // 本地直傳完成後，直接觸發與 webhook 等價的處理流程（掃毒 / 轉碼 / 標記 Ready）。
        await storageEvents.HandleObjectCreatedAsync(key, size, ct);

        return Ok();
    }

    /// <summary>提供本地儲存檔案的下載；<c>public/</c> 前綴免簽章，其餘須帶有效簽章。</summary>
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
