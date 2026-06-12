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
    IOptions<StorageOptions> storageOptions) : IFileService
{
    private static readonly HashSet<string> AllowedContentTypes =
    [
        "video/mp4", "video/quicktime", "video/x-msvideo", "video/webm",
        "image/jpeg", "image/png", "image/gif", "image/webp",
        "application/pdf",
    ];

    /// <inheritdoc/>
    public async Task<RequestUploadUrlResponse> RequestUploadUrlAsync(
        RequestUploadUrlRequest request, CancellationToken ct)
    {
        if (!AllowedContentTypes.Contains(request.ContentType))
            throw new ValidationException($"不支援的檔案類型：{request.ContentType}");

        var opts      = storageOptions.Value;
        var expiry    = TimeSpan.FromSeconds(opts.UploadUrlExpirySeconds);
        var expiresAt = DateTimeOffset.UtcNow.Add(expiry);

        var file = new StoredFile
        {
            CreatorId    = request.CreatorId,
            ProductId    = request.ProductId,
            OriginalName = request.OriginalName,
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

        return ToDto(file);
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

    private static FileDto ToDto(StoredFile f) => new()
    {
        Id           = f.Id,
        CreatorId    = f.CreatorId,
        ProductId    = f.ProductId,
        OriginalName = f.OriginalName,
        ContentType  = f.ContentType,
        SizeBytes    = f.SizeBytes,
        FileType     = f.FileType,
        Status       = f.Status,
        IsPreview    = f.IsPreview,
        CreatedAt    = f.CreatedAt,
        UpdatedAt    = f.UpdatedAt,
    };
}
