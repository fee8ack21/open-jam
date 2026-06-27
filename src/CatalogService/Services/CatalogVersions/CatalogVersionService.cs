using AutoMapper;
using AutoMapper.QueryableExtensions;
using CatalogService.Data;
using CatalogService.Data.Entities;
using CatalogService.Models;
using Microsoft.EntityFrameworkCore;
using Shared.Auth;
using Shared.Exceptions;

namespace CatalogService.Services.CatalogVersions;

/// <summary>商品版本業務邏輯實作。</summary>
public class CatalogVersionService(
    CatalogDbContext db,
    ICurrentUserAccessor currentUser,
    StorageServiceClient storageClient,
    StoreServiceClient storeClient,
    QuotaServiceClient quotaClient,
    OrderServiceClient orderClient,
    IMapper mapper) : ICatalogVersionService
{
    /// <inheritdoc/>
    public async Task<List<CatalogVersionDto>> ListAsync(Guid catalogId, CancellationToken ct)
    {
        await CatalogAuthorization.LoadOwnedCatalogAsync(db, storeClient, currentUser, catalogId, ct);

        var versions = await db.CatalogVersions.AsNoTracking()
            .Where(v => v.CatalogId == catalogId)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync(ct);

        var result = new List<CatalogVersionDto>(versions.Count);
        foreach (var version in versions)
            result.Add(await ToDtoAsync(version, ct));

        return result;
    }

    /// <inheritdoc/>
    public async Task<CatalogVersionDto> CreateAsync(Guid catalogId, CreateCatalogVersionRequest request, CancellationToken ct)
    {
        var catalog = await CatalogAuthorization.LoadOwnedCatalogAsync(db, storeClient, currentUser, catalogId, ct);

        var versionString = request.Version.Trim();

        var duplicate = await db.CatalogVersions
            .AnyAsync(v => v.CatalogId == catalogId && v.Version == versionString, ct);
        if (duplicate)
            throw new ConflictException("此版本字串於商品內已存在。");

        var version = new CatalogVersion
        {
            CatalogId = catalogId,
            Version = versionString,
            ReleaseNote = string.IsNullOrEmpty(request.ReleaseNote) ? null : request.ReleaseNote,
        };
        db.CatalogVersions.Add(version);

        // 新版本成為目前對外版本。
        catalog.CurrentVersionId = version.Id;

        await db.SaveChangesAsync(ct);

        return await ToDtoAsync(version, ct);
    }

    /// <inheritdoc/>
    public async Task<VersionAssetUploadUrlResponse> RequestAssetUploadUrlAsync(
        Guid catalogId, Guid versionId, RequestVersionAssetUploadUrlRequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException();
        var catalog = await CatalogAuthorization.LoadOwnedCatalogAsync(db, storeClient, currentUser, catalogId, ct);

        var version = await LoadVersionAsync(catalogId, versionId, ct);

        var fileType = CatalogAssetContentTypes.ResolveDownloadable(request.ContentType);

        // 簽發上傳 URL 前先向 QuotaService 預扣；後續任何失敗都釋放預扣。
        var reservationId = await quotaClient.ReserveAsync(request.SizeBytes, catalog.Id, ct);

        try
        {
            var result = await storageClient.RequestUploadUrlAsync(
                userId, catalog.Id, request.FileName, request.ContentType, request.SizeBytes,
                fileType, isPublic: false, reservationId, ct);

            var nextSortOrder = await db.CatalogVersionAssets
                .Where(a => a.CatalogVersionId == version.Id)
                .CountAsync(ct);

            var asset = new CatalogVersionAsset
            {
                Id = result.FileId,
                CatalogVersionId = version.Id,
                FileName = request.FileName,
                StorageKey = result.StorageKey,
                FileSize = request.SizeBytes,
                ContentType = request.ContentType,
                SortOrder = nextSortOrder,
            };
            db.CatalogVersionAssets.Add(asset);

            await db.SaveChangesAsync(ct);

            return new VersionAssetUploadUrlResponse
            {
                AssetId = asset.Id,
                UploadUrl = result.UploadUrl,
                ExpiresAt = result.ExpiresAt,
            };
        }
        catch
        {
            await quotaClient.ReleaseAsync(reservationId, ct);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<StorageDownloadUrlResult> GetAssetDownloadUrlAsync(
        Guid catalogId, Guid versionId, Guid assetId, CancellationToken ct)
    {
        await CatalogAuthorization.LoadOwnedCatalogAsync(db, storeClient, currentUser, catalogId, ct);
        await LoadVersionAsync(catalogId, versionId, ct);

        var exists = await db.CatalogVersionAssets
            .AnyAsync(a => a.Id == assetId && a.CatalogVersionId == versionId, ct);
        if (!exists)
            throw new NotFoundException("找不到檔案。");

        return await storageClient.GetDownloadUrlAsync(assetId, ct);
    }

    /// <inheritdoc/>
    public async Task<List<PurchasedVersionAssetDto>> ListPurchasedDownloadsAsync(
        Guid catalogId, Guid versionId, CancellationToken ct)
    {
        // 買家視角：須已購買（完成訂單）此商品；非 Owner 授權。
        if (!await orderClient.HasPurchasedAsync(catalogId, ct))
            throw new ForbiddenException("尚未購買此商品，無法下載。");

        await LoadVersionAsync(catalogId, versionId, ct);

        var assets = await db.CatalogVersionAssets.AsNoTracking()
            .Where(a => a.CatalogVersionId == versionId)
            .OrderBy(a => a.SortOrder)
            .ToListAsync(ct);

        var result = new List<PurchasedVersionAssetDto>(assets.Count);
        foreach (var asset in assets)
        {
            var signed = await storageClient.GetDownloadUrlAsync(asset.Id, ct);
            result.Add(new PurchasedVersionAssetDto
            {
                Id          = asset.Id,
                FileName    = asset.FileName,
                ContentType = asset.ContentType,
                FileSize    = asset.FileSize,
                SortOrder   = asset.SortOrder,
                DownloadUrl = signed.DownloadUrl,
                ExpiresAt   = signed.ExpiresAt,
            });
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task DeleteAssetAsync(Guid catalogId, Guid versionId, Guid assetId, CancellationToken ct)
    {
        await CatalogAuthorization.LoadOwnedCatalogAsync(db, storeClient, currentUser, catalogId, ct);
        await LoadVersionAsync(catalogId, versionId, ct);

        var asset = await db.CatalogVersionAssets
            .FirstOrDefaultAsync(a => a.Id == assetId && a.CatalogVersionId == versionId, ct)
            ?? throw new NotFoundException("找不到檔案。");

        db.CatalogVersionAssets.Remove(asset);
        await db.SaveChangesAsync(ct);
    }

    private async Task<CatalogVersion> LoadVersionAsync(Guid catalogId, Guid versionId, CancellationToken ct) =>
        await db.CatalogVersions.FirstOrDefaultAsync(v => v.Id == versionId && v.CatalogId == catalogId, ct)
            ?? throw new NotFoundException("找不到版本。");

    private async Task<CatalogVersionDto> ToDtoAsync(CatalogVersion version, CancellationToken ct)
    {
        var assets = await db.CatalogVersionAssets.AsNoTracking()
            .Where(a => a.CatalogVersionId == version.Id)
            .OrderBy(a => a.SortOrder)
            .ProjectTo<CatalogVersionAssetDto>(mapper.ConfigurationProvider)
            .ToListAsync(ct);

        var dto = mapper.Map<CatalogVersionDto>(version);
        dto.Assets = assets;
        return dto;
    }
}
