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
/// 2. 商品檔 Ready 後超過 UnreferencedRetentionDays 仍未被使用（未建立 reference）→ 軟刪除。
/// 3. 軟刪除超過 SoftDeleteRetentionDays → 從儲存後端永久刪除並移除 DB 紀錄。
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

        // 2. 商品檔上傳完成但逾期未被使用（未建立 reference，不計配額）→ 軟刪除回收。
        //    僅限關聯商品的檔案；商店 Avatar/Banner 等非商品檔不走 reference 流程，不在此清理。
        var unreferencedDeadline = DateTimeOffset.UtcNow.AddDays(-_opts.UnreferencedRetentionDays);
        var unreferenced = await db.StoredFiles
            .Where(f => f.Status == FileStatus.Ready
                     && f.ReferencedAt == null
                     && f.ProductId != null
                     && f.CreatedAt < unreferencedDeadline)
            .ToListAsync(ct);

        foreach (var file in unreferenced)
        {
            db.StoredFiles.Remove(file);
            logger.LogInformation("Unreferenced file {FileId} soft-deleted after retention", file.Id);
        }

        if (unreferenced.Count > 0)
            await db.SaveChangesAsync(ct);

        // 3. 軟刪除超過保留期 → 永久刪除（硬刪除）
        //    需 IgnoreQueryFilters 才能查到已軟刪除的資料（全域過濾器預設排除 DeletedAt != null）。
        var retentionDeadline = DateTimeOffset.UtcNow.AddDays(-_opts.SoftDeleteRetentionDays);
        var expired = await db.StoredFiles
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(f => f.DeletedAt != null && f.DeletedAt < retentionDeadline)
            .ToListAsync(ct);

        // 先刪儲存後端，成功者才從 DB 永久移除；失敗者保留紀錄，下個週期重試。
        var purgedIds = new List<Guid>();
        foreach (var file in expired)
        {
            try
            {
                await storage.DeleteAsync(file.StorageKey, file.IsPublic, ct);
                purgedIds.Add(file.Id);
                logger.LogInformation("Permanently deleted file {FileId} from storage", file.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete file {FileId} from storage backend", file.Id);
            }
        }

        // ExecuteDelete 直接對資料庫硬刪除，繞過 BaseDbContext 將 Remove() 轉為軟刪除的行為。
        if (purgedIds.Count > 0)
            await db.StoredFiles
                .IgnoreQueryFilters()
                .Where(f => purgedIds.Contains(f.Id))
                .ExecuteDeleteAsync(ct);
    }
}
