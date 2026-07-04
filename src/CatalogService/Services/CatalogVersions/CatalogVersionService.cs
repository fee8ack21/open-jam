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

        // 簽發階段不預扣配額、不建資產紀錄；使用者提交確認（confirm）時才建立 reference 並扣量。
        var result = await storageClient.RequestUploadUrlAsync(
            userId, catalog.Id, request.FileName, request.ContentType, request.SizeBytes,
            fileType, isPublic: false, ct);

        return new VersionAssetUploadUrlResponse
        {
            AssetId = result.FileId,
            UploadUrl = result.UploadUrl,
            ExpiresAt = result.ExpiresAt,
        };
    }

    /// <inheritdoc/>
    public async Task<CatalogVersionAssetDto> ConfirmAssetAsync(
        Guid catalogId, Guid versionId, Guid assetId, CancellationToken ct)
    {
        var catalog = await CatalogAuthorization.LoadOwnedCatalogAsync(db, storeClient, currentUser, catalogId, ct);
        var version = await LoadVersionAsync(catalogId, versionId, ct);

        // 1. 確認檔案已直傳完成並取得實際大小（冪等）。
        var file = await storageClient.ConfirmUploadAsync(assetId, ct);
        if (file.ProductId != catalog.Id)
            throw new ValidationException("檔案不屬於此商品。");
        if (file.Status != StorageFileStatus.Ready)
            throw new ValidationException("檔案尚未完成處理，請稍後再試。");

        // 2. 扣配額（冪等鍵 = 檔案 ID）；配額不足在建立 reference 前擋下。
        await quotaClient.ChargeAsync(assetId, file.SizeBytes ?? 0, catalog.Id, ct);

        // 3. 建立資產 reference（冪等：已存在則沿用）。
        var asset = await db.CatalogVersionAssets
            .FirstOrDefaultAsync(a => a.Id == assetId && a.CatalogVersionId == version.Id, ct);
        if (asset is null)
        {
            var nextSortOrder = await db.CatalogVersionAssets
                .Where(a => a.CatalogVersionId == version.Id)
                .CountAsync(ct);

            asset = new CatalogVersionAsset
            {
                Id = assetId,
                CatalogVersionId = version.Id,
                FileName = file.OriginalName,
                StorageKey = file.StorageKey,
                FileSize = file.SizeBytes ?? 0,
                ContentType = file.ContentType,
                SortOrder = nextSortOrder,
            };
            db.CatalogVersionAssets.Add(asset);

            await db.SaveChangesAsync(ct);
        }

        // 4. 標記檔案已被使用（冪等），未標記檔案逾期會被 StorageService 清理且不計配額。
        await storageClient.MarkReferencedAsync(assetId, ct);

        return mapper.Map<CatalogVersionAssetDto>(asset);
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
        Guid catalogId, Guid versionId, Guid? orderId, CancellationToken ct)
    {
        // 買家視角授權（非 Owner）：訪客憑訂單 ID（不可猜測的 GUID 作為下載憑證，
        // 隨訂單完成信寄給買家）驗證該訂單已完成且包含此商品；登入買家以購買紀錄驗證。
        if (orderId is Guid oid)
        {
            if (!await orderClient.OrderGrantsCatalogAsync(oid, catalogId, ct))
                throw new ForbiddenException("此訂單不存在、尚未完成或不包含此商品。");
        }
        else
        {
            if (currentUser.UserId is null)
                throw new UnauthorizedException();
            if (!await orderClient.HasPurchasedAsync(catalogId, ct))
                throw new ForbiddenException("尚未購買此商品，無法下載。");
        }

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

        // 先軟刪儲存端檔案（404 視為已刪，冪等），配額用量由 QuotaService 對帳釋放。
        await storageClient.DeleteFileAsync(assetId, ct);

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
