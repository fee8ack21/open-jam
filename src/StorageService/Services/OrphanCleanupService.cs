using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StorageService.Data;
using StorageService.Data.Entities;
using StorageService.Options;
using StorageService.Storage;

namespace StorageService.Services;

/// <summary>
/// 排程清理孤兒檔案的背景服務，每小時執行一次：
/// 1. 超過 UploadTimeoutHours 仍在 Uploading 狀態 → 標記 Failed（上傳逾時）。
/// 2. 軟刪除超過 SoftDeleteRetentionDays → 從儲存後端永久刪除並移除 DB 紀錄。
/// </summary>
public class OrphanCleanupService(
    IServiceScopeFactory scopeFactory,
    IOptions<StorageOptions> options,
    ILogger<OrphanCleanupService> logger) : BackgroundService
{
    private readonly StorageOptions _opts = options.Value;

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "OrphanCleanupService error");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task CleanupAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db      = scope.ServiceProvider.GetRequiredService<StorageDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IStorageProvider>();

        // 1. 上傳逾時：標記 Failed
        var uploadDeadline = DateTimeOffset.UtcNow.AddHours(-_opts.UploadTimeoutHours);
        var stale = await db.StoredFiles
            .Where(f => f.Status == FileStatus.Uploading
                     && f.CreatedAt < uploadDeadline
                     && f.DeletedAt == null)
            .ToListAsync(ct);

        foreach (var file in stale)
        {
            file.Status = FileStatus.Failed;
            logger.LogWarning("Upload timeout: marking file {FileId} as Failed", file.Id);
        }

        if (stale.Count > 0)
            await db.SaveChangesAsync(ct);

        // 2. 軟刪除超過保留期 → 永久刪除
        var retentionDeadline = DateTimeOffset.UtcNow.AddDays(-_opts.SoftDeleteRetentionDays);
        var expired = await db.StoredFiles
            .Where(f => f.DeletedAt != null && f.DeletedAt < retentionDeadline)
            .ToListAsync(ct);

        foreach (var file in expired)
        {
            try
            {
                await storage.DeleteAsync(file.StorageKey, ct);
                db.StoredFiles.Remove(file);
                logger.LogInformation("Permanently deleted file {FileId} from storage", file.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete file {FileId} from storage backend", file.Id);
            }
        }

        if (expired.Count > 0)
            await db.SaveChangesAsync(ct);
    }
}
