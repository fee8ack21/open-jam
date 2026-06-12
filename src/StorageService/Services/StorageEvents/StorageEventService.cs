using Microsoft.EntityFrameworkCore;
using StorageService.Data;

namespace StorageService.Services.StorageEvents;

/// <summary>儲存後端物件事件處理業務邏輯實作。</summary>
public class StorageEventService(
    StorageDbContext db,
    FileProcessingService processor,
    ILogger<StorageEventService> logger) : IStorageEventService
{
    /// <inheritdoc/>
    public async Task<bool> HandleObjectCreatedAsync(string objectKey, long? reportedSize, CancellationToken ct)
    {
        // StorageKey 格式：creators/{creatorId}/{fileId}/{originalName}
        var parts = objectKey.TrimStart('/').Split('/');
        if (parts.Length < 3 || !Guid.TryParse(parts[2], out var fileId))
        {
            logger.LogWarning("Unrecognised storage key format: {Key}", objectKey);
            return false;
        }

        var file = await db.StoredFiles
            .FirstOrDefaultAsync(f => f.Id == fileId && f.DeletedAt == null, ct);

        if (file is null)
        {
            logger.LogWarning("Received storage event for unknown file {FileId}", fileId);
            return true;
        }

        // 更新 SizeBytes（若儲存後端提供）
        if (reportedSize.HasValue && file.SizeBytes == null)
        {
            file.SizeBytes = reportedSize;
            await db.SaveChangesAsync(ct);
        }

        // 在背景執行 pipeline，不阻塞 webhook 回應（MinIO 期望快速回應）
        _ = Task.Run(() => processor.ProcessAsync(fileId, CancellationToken.None), ct);

        return true;
    }
}
