using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared.Events;
using StorageService.Data;
using StorageService.Data.Entities;

namespace StorageService.Services;

/// <summary>
/// 上傳完成後的異步處理 pipeline：掃毒 → 轉碼 / 預覽生成 → 標記 Ready。
/// MVP 階段各步驟為 stub，全部通過後直接標記 Ready；後續可插入真實 ClamAV / GCP Transcoder 實作。
/// </summary>
public class FileProcessingService(
    StorageDbContext db,
    IPublishEndpoint publishEndpoint,
    ILogger<FileProcessingService> logger)
{
    /// <summary>對指定檔案執行完整處理 pipeline；完成後發布 <see cref="FileReadyEvent"/>。</summary>
    public async Task ProcessAsync(Guid fileId, CancellationToken ct = default)
    {
        var file = await db.StoredFiles.FirstOrDefaultAsync(f => f.Id == fileId && f.DeletedAt == null, ct);

        if (file is null)
        {
            logger.LogWarning("ProcessAsync: file {FileId} not found", fileId);
            return;
        }

        if (file.Status != FileStatus.Uploading)
        {
            logger.LogInformation("ProcessAsync: file {FileId} already in status {Status}, skipping", fileId, file.Status);
            return;
        }

        file.Status = FileStatus.Processing;
        await db.SaveChangesAsync(ct);

        try
        {
            await ScanAsync(file, ct);
            await TranscodeAsync(file, ct);
            await GeneratePreviewAsync(file, ct);

            file.Status = FileStatus.Ready;
            await db.SaveChangesAsync(ct);

            await publishEndpoint.Publish(new FileReadyEvent(
                file.Id,
                file.CreatorId,
                file.ProductId,
                file.ContentType,
                file.FileType.ToString(),
                file.SizeBytes,
                file.IsPreview,
                file.ReservationId), ct);

            logger.LogInformation("File {FileId} processed and ready", fileId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Processing failed for file {FileId}", fileId);
            file.Status = FileStatus.Failed;
            await db.SaveChangesAsync(ct);
        }
    }

    // TODO: 替換為真實 ClamAV 整合（GKE worker）
    private Task ScanAsync(StoredFile file, CancellationToken ct)
    {
        logger.LogDebug("Malware scan stub: file {FileId} passed", file.Id);
        return Task.CompletedTask;
    }

    // TODO: 替換為 GCP Transcoder API 整合（僅影片）
    private Task TranscodeAsync(StoredFile file, CancellationToken ct)
    {
        if (file.FileType != FileType.Video) return Task.CompletedTask;
        logger.LogDebug("HLS transcode stub: file {FileId} skipped", file.Id);
        return Task.CompletedTask;
    }

    // TODO: 依 FileType 生成試看片段 / PDF 預覽頁 / 縮圖
    private Task GeneratePreviewAsync(StoredFile file, CancellationToken ct)
    {
        logger.LogDebug("Preview generation stub: file {FileId} skipped", file.Id);
        return Task.CompletedTask;
    }
}
