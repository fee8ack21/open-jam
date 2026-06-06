using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorageService.Data;
using StorageService.Services;

namespace StorageService.Controllers;

/// <summary>
/// 供 MinIO Bucket Notification Webhook 呼叫的內部端點，不對外公開。
/// 設定方式：MinIO Console → Buckets → Events → Webhook → 填入此端點 URL。
/// </summary>
[ApiController]
[Route("internal")]
public class InternalController(
    StorageDbContext db,
    FileProcessingService processor,
    ILogger<InternalController> logger) : ControllerBase
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

        var objectKey = notification.Records[0].S3?.Object?.Key;
        if (string.IsNullOrEmpty(objectKey))
            return BadRequest("missing object key");

        // StorageKey 格式：creators/{creatorId}/{fileId}/{originalName}
        var parts = objectKey.TrimStart('/').Split('/');
        if (parts.Length < 3 || !Guid.TryParse(parts[2], out var fileId))
        {
            logger.LogWarning("Unrecognised storage key format: {Key}", objectKey);
            return BadRequest("unrecognised key format");
        }

        var file = await db.StoredFiles
            .FirstOrDefaultAsync(f => f.Id == fileId && f.DeletedAt == null, ct);

        if (file is null)
        {
            logger.LogWarning("Received storage event for unknown file {FileId}", fileId);
            return NoContent();
        }

        // 更新 SizeBytes（若 MinIO 提供）
        var reportedSize = notification.Records[0].S3?.Object?.Size;
        if (reportedSize.HasValue && file.SizeBytes == null)
        {
            file.SizeBytes = reportedSize;
            await db.SaveChangesAsync(ct);
        }

        // 在背景執行 pipeline，不阻塞 webhook 回應（MinIO 期望快速回應）
        _ = Task.Run(() => processor.ProcessAsync(fileId, CancellationToken.None), ct);

        return NoContent();
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
