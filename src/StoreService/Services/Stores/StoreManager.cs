using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shared.Auth;
using Shared.Exceptions;
using StoreService.Data;
using StoreService.Data.Entities;
using StoreService.Models;
using StoreService.Options;

namespace StoreService.Services.Stores;

/// <summary>商店業務邏輯實作。</summary>
public class StoreManager(
    StoreDbContext db,
    ICurrentUserAccessor currentUser,
    IOptions<StorageOptions> storageOptions,
    StorageServiceClient storageClient,
    AuditLogPublisher auditLog,
    IHttpContextAccessor httpContextAccessor,
    IMapper mapper) : IStoreManager
{
    private readonly string _publicBaseUrl = (storageOptions.Value.PublicBaseUrl ?? "").TrimEnd('/');

    /// <inheritdoc/>
    public async Task<StoreDto> GetAsync(string idOrSlug, CancellationToken ct)
    {
        var store = await FindStoreAsync(idOrSlug, ct)
            ?? throw new NotFoundException("找不到商店。");

        return await ToDtoAsync(store, ct);
    }

    /// <inheritdoc/>
    public async Task<List<MyStoreDto>> GetMineAsync(CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException();

        var memberships = await db.StoreMembers.AsNoTracking()
            .Where(m => m.UserId == userId)
            .Join(db.Stores.AsNoTracking(), m => m.StoreId, s => s.Id, (m, s) => new { m.Role, Store = s })
            .ToListAsync(ct);

        var result = new List<MyStoreDto>();
        foreach (var item in memberships)
            result.Add(new MyStoreDto { Store = await ToDtoAsync(item.Store, ct), Role = item.Role });

        return result;
    }

    /// <inheritdoc/>
    public async Task<StoreDto> UpdateAsync(Guid id, UpdateStoreRequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException();

        var store = await db.Stores.FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new NotFoundException("找不到商店。");

        await StoreAuthorization.EnsureOwnerAsync(db, id, userId, ct);

        if (request.StoreName is not null)
            store.StoreName = request.StoreName.Trim();

        if (request.Description is not null)
            store.Description = request.Description.Length == 0 ? null : request.Description;

        await db.SaveChangesAsync(ct);

        return await ToDtoAsync(store, ct);
    }

    /// <inheritdoc/>
    public async Task SuspendAsync(Guid id, CancellationToken ct)
    {
        var store = await db.Stores.FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new NotFoundException("找不到商店。");

        if (store.Status != StoreStatus.Active)
            throw new ValidationException("僅 Active 狀態的商店可停權。");

        store.Status = StoreStatus.Suspended;

        auditLog.Add(currentUser.UserId, "store.suspend", "Store", id, tenant: id);

        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task UnsuspendAsync(Guid id, CancellationToken ct)
    {
        var store = await db.Stores.FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new NotFoundException("找不到商店。");

        if (store.Status != StoreStatus.Suspended)
            throw new ValidationException("僅 Suspended 狀態的商店可解除停權。");

        store.Status = StoreStatus.Active;

        auditLog.Add(currentUser.UserId, "store.unsuspend", "Store", id, tenant: id);

        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task CloseAsync(Guid id, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException();

        var store = await db.Stores.FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new NotFoundException("找不到商店。");

        var isAdmin = httpContextAccessor.HttpContext?.User.IsInRole("Admin") ?? false;
        if (!isAdmin)
            await StoreAuthorization.EnsureOwnerAsync(db, id, userId, ct);

        if (store.Status == StoreStatus.Closed)
            throw new ValidationException("商店已關閉。");

        store.Status = StoreStatus.Closed;

        auditLog.Add(userId, "store.close", "Store", id, tenant: id);

        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<AssetUploadUrlResponse> RequestAssetUploadUrlAsync(
        Guid id, RequestAssetUploadUrlRequest request, bool isAvatar, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException();

        var store = await db.Stores.FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new NotFoundException("找不到商店。");

        await StoreAuthorization.EnsureOwnerAsync(db, id, userId, ct);

        var result = await storageClient.RequestPublicImageUploadUrlAsync(
            userId, request.FileName, request.ContentType, request.SizeBytes, ct);

        var asset = new Asset
        {
            Id = result.FileId,
            CreatedBy = userId,
            StorageKey = result.StorageKey,
            FileName = request.FileName,
            ContentType = request.ContentType,
            FileSize = request.SizeBytes,
        };
        db.Assets.Add(asset);

        if (isAvatar)
            store.AvatarAssetId = asset.Id;
        else
            store.BannerAssetId = asset.Id;

        await db.SaveChangesAsync(ct);

        return new AssetUploadUrlResponse
        {
            AssetId = asset.Id,
            UploadUrl = result.UploadUrl,
            PublicUrl = result.PublicUrl ?? $"{_publicBaseUrl}/{result.StorageKey}",
            ExpiresAt = result.ExpiresAt,
        };
    }

    private async Task<Store?> FindStoreAsync(string idOrSlug, CancellationToken ct) =>
        Guid.TryParse(idOrSlug, out var id)
            ? await db.Stores.FirstOrDefaultAsync(s => s.Id == id, ct)
            : await db.Stores.FirstOrDefaultAsync(s => s.StoreSlug == idOrSlug, ct);

    private async Task<StoreDto> ToDtoAsync(Store store, CancellationToken ct)
    {
        var dto = mapper.Map<StoreDto>(store);
        dto.AvatarUrl = await GetAssetUrlAsync(store.AvatarAssetId, ct);
        dto.BannerUrl = await GetAssetUrlAsync(store.BannerAssetId, ct);
        return dto;
    }

    private async Task<string?> GetAssetUrlAsync(Guid? assetId, CancellationToken ct)
    {
        if (assetId is null)
            return null;

        var storageKey = await db.Assets.AsNoTracking()
            .Where(a => a.Id == assetId)
            .Select(a => a.StorageKey)
            .FirstOrDefaultAsync(ct);

        return storageKey is null ? null : $"{_publicBaseUrl}/{storageKey}";
    }
}
