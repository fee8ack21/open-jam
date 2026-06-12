using Microsoft.AspNetCore.Mvc;
using StorageService.Services.StorageEvents;

namespace StorageService.Controllers;

/// <summary>
/// 供 MinIO Bucket Notification Webhook 呼叫的內部端點，不對外公開。
/// 設定方式：MinIO Console → Buckets → Events → Webhook → 填入此端點 URL。
/// </summary>
[ApiController]
[Route("internal")]
public class InternalController(IStorageEventService storageEvents) : ControllerBase
{
    /// <summary>
    /// 接收 MinIO S3-compatible bucket event notification（ObjectCreated）。
    /// MinIO 直傳完成後自動 POST 此端點，觸發掃毒 / 轉碼 / 預覽生成 pipeline。
    /// </summary>
    /// <remarks>
    /// MinIO 事件格式（S3 compatible）：
    /// <code>
    /// {
    ///   "EventName": "s3:ObjectCreated:Put",
    ///   "Key": "{bucket}/{storageKey}",
    ///   "Records": [{ "s3": { "object": { "key": "{storageKey}", "size": 1234 } } }]
    /// }
    /// </code>
    /// StorageKey 格式：creators/{creatorId}/{fileId}/{originalName}
    /// </remarks>
    [HttpPost("storage-event")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StorageEventAsync(
        [FromBody] MinioNotification notification, CancellationToken ct)
    {
        if (notification.Records is not { Count: > 0 })
            return BadRequest("no records");

        var s3Object = notification.Records[0].S3?.Object;
        if (string.IsNullOrEmpty(s3Object?.Key))
            return BadRequest("missing object key");

        var handled = await storageEvents.HandleObjectCreatedAsync(s3Object.Key, s3Object.Size, ct);
        return handled ? NoContent() : BadRequest("unrecognised key format");
    }
}

#region MinIO Notification Models

/// <summary>MinIO S3-compatible 事件通知根節點。</summary>
public class MinioNotification
{
    /// <summary>事件名稱，例如 "s3:ObjectCreated:Put"。</summary>
    public string? EventName { get; set; }

    /// <summary>事件紀錄清單。</summary>
    public List<MinioRecord>? Records { get; set; }
}

/// <summary>單筆 MinIO 事件紀錄。</summary>
public class MinioRecord
{
    /// <summary>S3 物件資訊。</summary>
    public MinioS3? S3 { get; set; }
}

/// <summary>MinIO S3 事件的 S3 節點。</summary>
public class MinioS3
{
    /// <summary>S3 物件資訊。</summary>
    public MinioS3Object? Object { get; set; }
}

/// <summary>MinIO S3 物件資訊。</summary>
public class MinioS3Object
{
    /// <summary>物件鍵值（不含 bucket 名稱）。</summary>
    public string? Key { get; set; }

    /// <summary>物件大小（bytes）。</summary>
    public long? Size { get; set; }
}

#endregion
