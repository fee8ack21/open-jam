using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shared.Exceptions;
using StorageService.Data;
using StorageService.Data.Entities;
using StorageService.Models;
using StorageService.Options;
using StorageService.Storage;

namespace StorageService.Services.Files;

/// <summary>數位商品檔案業務邏輯實作。</summary>
public class FileService(
    StorageDbContext db,
    IStorageProvider storage,
    IOptions<StorageOptions> storageOptions,
    FileProcessingService processor,
    IMapper mapper) : IFileService
{
    /// <inheritdoc/>
    public async Task<RequestUploadUrlResponse> RequestUploadUrlAsync(
        RequestUploadUrlRequest request, CancellationToken ct)
    {
        var opts      = storageOptions.Value;

        // 單檔大小上限於簽發階段即擋（涵蓋所有 provider 的單一收斂點）；
        // 地端另於 blob 端點以簽章綁定強制，GCS 殘餘超量由 confirm 配額檢查與孤兒清理兜底。
        if (request.SizeBytes > opts.MaxUploadBytes)
            throw new ValidationException($"檔案大小超過單檔上限（{opts.MaxUploadBytes} bytes）。");

        // 待確認量護欄：已上傳但尚未 reference（不計配額）的暫存量不得無限堆積。
        var pendingBytes = await db.StoredFiles
            .Where(f => f.CreatorId == request.CreatorId
                     && f.ReferencedAt == null
                     && (f.Status == FileStatus.Uploading
                      || f.Status == FileStatus.Processing
                      || f.Status == FileStatus.Ready))
            .SumAsync(f => f.SizeBytes ?? 0, ct);
        if (pendingBytes + request.SizeBytes > opts.MaxPendingBytes)
            throw new ConflictException(
                $"待確認上傳量已達上限（{opts.MaxPendingBytes} bytes）；請先完成或移除既有未確認上傳。");

        var expiry    = TimeSpan.FromSeconds(opts.UploadUrlExpirySeconds);
        var expiresAt = DateTimeOffset.UtcNow.Add(expiry);

        var file = new StoredFile
        {
            CreatorId     = request.CreatorId,
            ProductId     = request.ProductId,
            OriginalName  = request.OriginalName,
            ContentType  = request.ContentType,
            SizeBytes    = request.SizeBytes,
            FileType     = request.FileType,
            IsPreview    = request.IsPreview,
        };
        file.StorageKey = request.IsPublic
            ? $"public/{request.CreatorId}/{file.Id}/{request.OriginalName}"
            : $"creators/{request.CreatorId}/{file.Id}/{request.OriginalName}";

        db.StoredFiles.Add(file);
        await db.SaveChangesAsync(ct);

        var uploadUrl = await storage.GenerateUploadUrlAsync(
            file.StorageKey, request.ContentType, request.SizeBytes, expiry, ct);

        var publicUrl = request.IsPublic
            ? $"{opts.PublicBaseUrl.TrimEnd('/')}/{file.StorageKey}"
            : null;

        return new RequestUploadUrlResponse
        {
            FileId     = file.Id,
            UploadUrl  = uploadUrl,
            StorageKey = file.StorageKey,
            PublicUrl  = publicUrl,
            ExpiresAt  = expiresAt,
        };
    }

    /// <inheritdoc/>
    public async Task<FileDto> GetAsync(Guid id, CancellationToken ct)
    {
        var file = await db.StoredFiles
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id && f.DeletedAt == null, ct)
            ?? throw new NotFoundException($"檔案 {id} 不存在");

        return mapper.Map<FileDto>(file);
    }

    /// <inheritdoc/>
    public async Task<FileDto> ConfirmUploadAsync(Guid id, CancellationToken ct)
    {
        var file = await db.StoredFiles
            .FirstOrDefaultAsync(f => f.Id == id && f.DeletedAt == null, ct)
            ?? throw new NotFoundException($"檔案 {id} 不存在");

        // 冪等：僅 Uploading 狀態需確認並觸發處理；其餘狀態回傳現狀。
        if (file.Status != FileStatus.Uploading)
            return mapper.Map<FileDto>(file);

        if (!await storage.ObjectExistsAsync(file.StorageKey, ct))
            throw new ValidationException($"檔案 {id} 尚未上傳至儲存後端");

        // 同步執行處理 pipeline（掃毒 / 轉碼 / 預覽，MVP 為 stub）後標記 Ready 並發 FileReadyEvent。
        await processor.ProcessAsync(id, ct);

        // 重新讀取以反映處理後的最新狀態。
        await db.Entry(file).ReloadAsync(ct);
        return mapper.Map<FileDto>(file);
    }

    /// <inheritdoc/>
    public async Task<FileDto> MarkReferencedAsync(Guid id, CancellationToken ct)
    {
        var file = await db.StoredFiles
            .FirstOrDefaultAsync(f => f.Id == id && f.DeletedAt == null, ct)
            ?? throw new NotFoundException($"檔案 {id} 不存在");

        if (file.Status != FileStatus.Ready)
            throw new ValidationException($"檔案 {id} 尚未完成處理（狀態：{file.Status}），無法標記為已使用");

        // 冪等：已標記則直接回傳現狀。
        if (file.ReferencedAt is null)
        {
            file.ReferencedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        return mapper.Map<FileDto>(file);
    }

    /// <inheritdoc/>
    public async Task<GetDownloadUrlResponse> GetDownloadUrlAsync(Guid id, CancellationToken ct)
    {
        var file = await db.StoredFiles
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id && f.DeletedAt == null, ct)
            ?? throw new NotFoundException($"檔案 {id} 不存在");

        if (file.Status != FileStatus.Ready)
            throw new ValidationException($"檔案 {id} 尚未完成處理（狀態：{file.Status}）");

        var opts   = storageOptions.Value;
        var expiry = TimeSpan.FromSeconds(opts.DownloadUrlExpirySeconds);

        var downloadUrl = await storage.GenerateDownloadUrlAsync(file.StorageKey, expiry, ct);

        return new GetDownloadUrlResponse
        {
            FileId      = file.Id,
            DownloadUrl = downloadUrl,
            ExpiresAt   = DateTimeOffset.UtcNow.Add(expiry),
        };
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var file = await db.StoredFiles
            .FirstOrDefaultAsync(f => f.Id == id && f.DeletedAt == null, ct)
            ?? throw new NotFoundException($"檔案 {id} 不存在");

        // BaseDbContext 會將 Remove() 自動轉為軟刪除並填入 DeletedAt / DeletedBy。
        db.StoredFiles.Remove(file);
        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<TenantUsageResponse> GetTenantUsageAsync(Guid creatorId, CancellationToken ct)
    {
        // 全域 Query Filter 已排除軟刪除；僅加總已 Ready 且已被使用（referenced）的檔案，
        // 上傳完成但未經功能 API 確認使用的檔案不計入配額。
        var total = await db.StoredFiles
            .AsNoTracking()
            .Where(f => f.CreatorId == creatorId && f.Status == FileStatus.Ready && f.ReferencedAt != null)
            .SumAsync(f => f.SizeBytes ?? 0, ct);

        return new TenantUsageResponse { CreatorId = creatorId, TotalBytes = total };
    }

    /// <inheritdoc/>
    public async Task<PlatformUsageResponse> GetPlatformUsageAsync(CancellationToken ct)
    {
        // 全域 Query Filter 已排除軟刪除。已 Ready 檔案才計入容量；公開 = 預覽 / 縮圖（IsPreview）。
        var ready = db.StoredFiles.AsNoTracking().Where(f => f.Status == FileStatus.Ready);

        var publicBytes = await ready.Where(f => f.IsPreview).SumAsync(f => f.SizeBytes ?? 0, ct);
        var privateBytes = await ready.Where(f => !f.IsPreview).SumAsync(f => f.SizeBytes ?? 0, ct);
        var fileCount = await ready.CountAsync(ct);

        // 孤兒檔：上傳未完成（Uploading）或處理失敗（Failed），保留期過後由 OrphanCleanupService 清理。
        var orphans = db.StoredFiles.AsNoTracking()
            .Where(f => f.Status == FileStatus.Uploading || f.Status == FileStatus.Failed);
        var orphanFileCount = await orphans.CountAsync(ct);
        var orphanBytes = await orphans.SumAsync(f => f.SizeBytes ?? 0, ct);

        var byCreator = await ready
            .GroupBy(f => f.CreatorId)
            .Select(g => new CreatorUsageDto
            {
                CreatorId = g.Key,
                FileCount = g.Count(),
                Bytes = g.Sum(f => f.SizeBytes ?? 0),
            })
            .OrderByDescending(c => c.Bytes)
            .Take(10)
            .ToListAsync(ct);

        return new PlatformUsageResponse
        {
            UsedBytes = publicBytes + privateBytes,
            FileCount = fileCount,
            PublicBytes = publicBytes,
            PrivateBytes = privateBytes,
            OrphanFileCount = orphanFileCount,
            OrphanBytes = orphanBytes,
            ByCreator = byCreator,
        };
    }
}
