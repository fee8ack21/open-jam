using AutoMapper;
using AutoMapper.QueryableExtensions;
using CatalogService.Data;
using CatalogService.Data.Entities;
using CatalogService.Models;
using CatalogService.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shared.Auth;
using Shared.Exceptions;

namespace CatalogService.Services.Catalogs;

/// <summary>商品業務邏輯實作。</summary>
public class CatalogManager(
    CatalogDbContext db,
    ICurrentUserAccessor currentUser,
    IOptions<StorageOptions> storageOptions,
    StorageServiceClient storageClient,
    StoreServiceClient storeClient,
    QuotaServiceClient quotaClient,
    PaymentServiceClient paymentClient,
    AuditLogPublisher auditLog,
    CatalogEventPublisher eventPublisher,
    IMapper mapper) : ICatalogManager
{
    private readonly string _publicBaseUrl = (storageOptions.Value.PublicBaseUrl ?? "").TrimEnd('/');

    /// <inheritdoc/>
    public async Task<CatalogDto> CreateAsync(CreateCatalogRequest request, CancellationToken ct)
    {
        _ = currentUser.UserId ?? throw new UnauthorizedException();
        await storeClient.EnsureStoreOwnerAsync(request.StoreId, ct);

        var name = request.Name.Trim();

        var slug = request.Slug.Trim().ToLowerInvariant();
        await CatalogSlugValidator.EnsureCatalogSlugUniqueAsync(db, request.StoreId, slug, null, ct);

        var currency = NormalizeCurrency(request.Currency);

        if (request.CategoryId is { } categoryId)
            await EnsureCategoryExistsAsync(categoryId, ct);

        var catalog = new Catalog
        {
            StoreId = request.StoreId,
            Name = name,
            Slug = slug,
            Description = string.IsNullOrEmpty(request.Description) ? null : request.Description,
            Summary = string.IsNullOrWhiteSpace(request.Summary) ? null : request.Summary.Trim(),
            CoverHue = request.CoverHue ?? 256,
            CategoryId = request.CategoryId,
            Price = request.Price,
            Currency = currency,
            Status = CatalogStatus.Draft,
        };
        db.Catalogs.Add(catalog);

        if (request.Tags is { Count: > 0 })
            await ApplyTagsAsync(catalog.Id, NormalizeTagNames(request.Tags), ct);

        auditLog.Add(currentUser.UserId, "catalog.create", "Catalog", catalog.Id, tenant: request.StoreId);

        await db.SaveChangesAsync(ct);

        return await ToDtoAsync(catalog, ct);
    }

    /// <inheritdoc/>
    public async Task<CatalogDto> GetAsync(Guid id, CancellationToken ct)
    {
        var catalog = await db.Catalogs.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException("找不到商品。");

        // 未上架商品僅 Owner 可見。
        if (catalog.Status != CatalogStatus.Published)
        {
            _ = currentUser.UserId ?? throw new NotFoundException("找不到商品。");
            await storeClient.EnsureStoreOwnerAsync(catalog.StoreId, ct);
        }

        return await ToDtoAsync(catalog, ct);
    }

    /// <inheritdoc/>
    public async Task<ListCatalogsResponse> ListAsync(ListCatalogsRequest request, bool publishedOnly, CancellationToken ct)
    {
        var query = db.Catalogs.AsNoTracking().AsQueryable();

        if (publishedOnly)
            query = query.Where(c => c.Status == CatalogStatus.Published);
        else if (request.Status is { } status)
            query = query.Where(c => c.Status == status);

        if (request.StoreId is { } storeId)
            query = query.Where(c => c.StoreId == storeId);

        if (request.CategoryId is { } categoryId)
            query = query.Where(c => c.CategoryId == categoryId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(c => EF.Functions.ILike(c.Name, $"%{term}%"));
        }

        if (request.Featured is { } featured)
            query = query.Where(c => c.IsFeatured == featured);

        if (request.StoreFeatured is { } storeFeatured)
            query = query.Where(c => c.IsStoreFeatured == storeFeatured);

        if (!string.IsNullOrWhiteSpace(request.Tag))
        {
            var tag = request.Tag.Trim().ToLowerInvariant();
            query =
                from c in query
                join m in db.CatalogTagMappings on c.Id equals m.CatalogId
                join t in db.CatalogTags on m.TagId equals t.Id
                where t.Name == tag
                select c;
        }

        if (request.MinPrice is { } minPrice)
            query = query.Where(c => c.Price >= minPrice);

        if (request.MaxPrice is { } maxPrice)
            query = query.Where(c => c.Price <= maxPrice);

        var total = await query.CountAsync(ct);

        // 價格排序仍以上架時間為次序鍵，確保同價商品有穩定順序。
        query = request.Sort switch
        {
            CatalogSort.PriceLowToHigh => query.OrderBy(c => c.Price).ThenByDescending(c => c.PublishedAt ?? c.CreatedAt),
            CatalogSort.PriceHighToLow => query.OrderByDescending(c => c.Price).ThenByDescending(c => c.PublishedAt ?? c.CreatedAt),
            CatalogSort.StoreFeatured => query.OrderBy(c => c.StoreFeaturedSortOrder).ThenByDescending(c => c.PublishedAt ?? c.CreatedAt),
            _ => query.OrderByDescending(c => c.PublishedAt ?? c.CreatedAt),
        };

        var catalogs = await query
            .Skip(request.Offset)
            .Take(request.Limit)
            .ToListAsync(ct);

        // 本頁商品標籤一次查回，避免逐筆 N+1。
        var ids = catalogs.Select(c => c.Id).ToList();
        var tagPairs = await db.CatalogTagMappings.AsNoTracking()
            .Where(m => ids.Contains(m.CatalogId))
            .Join(db.CatalogTags.AsNoTracking(), m => m.TagId, t => t.Id, (m, t) => new { m.CatalogId, t.Name })
            .ToListAsync(ct);
        var tagsByCatalog = tagPairs
            .GroupBy(x => x.CatalogId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name).OrderBy(n => n, StringComparer.Ordinal).ToList());

        var items = new List<CatalogSummaryDto>(catalogs.Count);
        foreach (var c in catalogs)
        {
            var summary = mapper.Map<CatalogSummaryDto>(c);
            summary.ThumbnailUrl = await GetAssetUrlAsync(c.ThumbnailAssetId, ct);
            summary.Tags = tagsByCatalog.GetValueOrDefault(c.Id) ?? [];
            items.Add(summary);
        }

        return new ListCatalogsResponse { TotalCount = total, Items = items };
    }

    /// <inheritdoc/>
    public async Task<CatalogDto> UpdateAsync(Guid id, UpdateCatalogRequest request, CancellationToken ct)
    {
        var catalog = await LoadOwnedCatalogAsync(id, ct);

        if (request.Name is not null)
            catalog.Name = request.Name.Trim();

        if (request.Slug is not null)
        {
            var slug = request.Slug.Trim().ToLowerInvariant();
            await CatalogSlugValidator.EnsureCatalogSlugUniqueAsync(db, catalog.StoreId, slug, catalog.Id, ct);
            catalog.Slug = slug;
        }

        if (request.Description is not null)
            catalog.Description = request.Description.Length == 0 ? null : request.Description;

        if (request.Summary is not null)
            catalog.Summary = request.Summary.Trim().Length == 0 ? null : request.Summary.Trim();

        if (request.CoverHue is { } coverHue)
            catalog.CoverHue = coverHue;

        if (request.Price is { } price)
        {
            // 已上架商品由免費改為付費時，比照上架閘門確認商店已完成收款設定。
            if (price > 0 && catalog.Price == 0 && catalog.Status == CatalogStatus.Published)
                await paymentClient.EnsurePayoutReadyAsync(catalog.StoreId, ct);

            catalog.Price = price;
        }

        if (request.Currency is not null)
            catalog.Currency = NormalizeCurrency(request.Currency);

        await db.SaveChangesAsync(ct);

        return await ToDtoAsync(catalog, ct);
    }

    /// <inheritdoc/>
    public async Task<CatalogDto> SetCategoryAsync(Guid id, SetCatalogCategoryRequest request, CancellationToken ct)
    {
        var catalog = await LoadOwnedCatalogAsync(id, ct);

        if (request.CategoryId is { } categoryId)
            await EnsureCategoryExistsAsync(categoryId, ct);

        catalog.CategoryId = request.CategoryId;

        await db.SaveChangesAsync(ct);

        return await ToDtoAsync(catalog, ct);
    }

    /// <inheritdoc/>
    public async Task<List<string>> SetTagsAsync(Guid id, SetCatalogTagsRequest request, CancellationToken ct)
    {
        var catalog = await LoadOwnedCatalogAsync(id, ct);

        var names = NormalizeTagNames(request.Tags);
        await ApplyTagsAsync(catalog.Id, names, ct);

        await db.SaveChangesAsync(ct);

        return names;
    }

    /// <inheritdoc/>
    public async Task PublishAsync(Guid id, CancellationToken ct)
    {
        var catalog = await LoadOwnedCatalogAsync(id, ct);

        if (catalog.Status == CatalogStatus.Suspended)
            throw new ValidationException("已停權的商品無法自行上架。");
        if (catalog.Status == CatalogStatus.Published)
            throw new ValidationException("商品已上架。");
        if (catalog.CurrentVersionId is null)
            throw new ValidationException("上架前須先建立至少一個版本。");

        // 付費商品上架閘門：商店須已完成 Stripe Connect 收款設定（免費商品不受限）。
        if (catalog.Price > 0)
            await paymentClient.EnsurePayoutReadyAsync(catalog.StoreId, ct);

        // 先佔用上架商品數額度（超上限拋 409）；若後續儲存失敗則補償回退。
        await quotaClient.ChangeProductCountAsync(1, ct);

        try
        {
            var isFirstPublish = catalog.PublishedAt is null;

            catalog.Status = CatalogStatus.Published;
            catalog.PublishedAt ??= DateTimeOffset.UtcNow;

            auditLog.Add(currentUser.UserId, "catalog.publish", "Catalog", catalog.Id, tenant: catalog.StoreId);
            eventPublisher.AddCatalogPublished(catalog, isFirstPublish);

            await db.SaveChangesAsync(ct);
        }
        catch
        {
            await quotaClient.ChangeProductCountAsync(-1, ct);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task ArchiveAsync(Guid id, CancellationToken ct)
    {
        var catalog = await LoadOwnedCatalogAsync(id, ct);

        if (catalog.Status != CatalogStatus.Published)
            throw new ValidationException("僅已上架的商品可下架封存。");

        catalog.Status = CatalogStatus.Archived;

        auditLog.Add(currentUser.UserId, "catalog.archive", "Catalog", catalog.Id, tenant: catalog.StoreId);

        await db.SaveChangesAsync(ct);

        // 離開 Published 狀態，釋放上架商品數額度（best-effort）。
        await quotaClient.ChangeProductCountAsync(-1, ct);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var catalog = await LoadOwnedCatalogAsync(id, ct);

        // 上架過的商品可能已有訂單 / 評論等下游資料，僅允許刪除未曾上架的草稿。
        if (catalog.Status != CatalogStatus.Draft || catalog.PublishedAt is not null)
            throw new ConflictException("僅未曾上架的草稿商品可刪除。");

        // 草稿已上傳的檔案（展示型資產 + 版本可下載檔）逐一軟刪儲存端檔案
        // （404 視為已刪，冪等），配額用量由 QuotaService 對帳釋放。
        var assets = await db.CatalogAssets
            .Where(a => a.CatalogId == catalog.Id)
            .ToListAsync(ct);

        var versions = await db.CatalogVersions
            .Where(v => v.CatalogId == catalog.Id)
            .ToListAsync(ct);
        var versionIds = versions.Select(v => v.Id).ToList();
        var versionAssets = await db.CatalogVersionAssets
            .Where(a => versionIds.Contains(a.CatalogVersionId))
            .ToListAsync(ct);

        foreach (var fileId in assets.Select(a => a.Id).Concat(versionAssets.Select(a => a.Id)))
            await storageClient.DeleteFileAsync(fileId, ct);

        // 附屬資料（資產 / 版本 / 標籤關聯）硬刪除並釋放標籤 UsageCount；商品主檔軟刪除。
        await ApplyTagsAsync(catalog.Id, [], ct);
        db.CatalogVersionAssets.RemoveRange(versionAssets);
        db.CatalogVersions.RemoveRange(versions);
        db.CatalogAssets.RemoveRange(assets);
        db.Catalogs.Remove(catalog);

        auditLog.Add(currentUser.UserId, "catalog.delete", "Catalog", catalog.Id, tenant: catalog.StoreId);

        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task SuspendAsync(Guid id, CancellationToken ct)
    {
        var catalog = await db.Catalogs.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException("找不到商品。");

        if (catalog.Status == CatalogStatus.Suspended)
            throw new ValidationException("商品已處於停權狀態。");

        catalog.Status = CatalogStatus.Suspended;

        auditLog.Add(currentUser.UserId, "catalog.suspend", "Catalog", catalog.Id, tenant: catalog.StoreId);

        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task UnsuspendAsync(Guid id, CancellationToken ct)
    {
        var catalog = await db.Catalogs.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException("找不到商品。");

        if (catalog.Status != CatalogStatus.Suspended)
            throw new ValidationException("僅停權中的商品可解除停權。");

        // 解除停權後回到封存狀態，由 Owner 自行決定是否重新上架。
        catalog.Status = CatalogStatus.Archived;

        auditLog.Add(currentUser.UserId, "catalog.unsuspend", "Catalog", catalog.Id, tenant: catalog.StoreId);

        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public Task IncrementViewAsync(Guid id, CancellationToken ct) =>
        // 高頻寫入，直接原子累加（不經 SaveChanges、不寫 audit、不動 UpdatedAt）；查無此商品則影響 0 列。
        db.Catalogs
            .Where(c => c.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.ViewCount, c => c.ViewCount + 1), ct);

    /// <inheritdoc/>
    public async Task SetFeaturedAsync(Guid id, bool featured, CancellationToken ct)
    {
        var catalog = await db.Catalogs.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException("找不到商品。");

        if (catalog.IsFeatured == featured)
            return; // 冪等：狀態未變則不重複寫入 / 記錄。

        catalog.IsFeatured = featured;

        auditLog.Add(currentUser.UserId, featured ? "catalog.feature" : "catalog.unfeature",
            "Catalog", catalog.Id, tenant: catalog.StoreId);

        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task SetStoreFeaturedAsync(Guid id, bool featured, CancellationToken ct)
    {
        var catalog = await LoadOwnedCatalogAsync(id, ct);

        // 僅已上架商品可設為店長精選（店面 spotlight 只展示上架商品）。取消精選則不限狀態。
        if (featured && catalog.Status != CatalogStatus.Published)
            throw new ConflictException("僅已上架商品可設為店長精選。");

        if (catalog.IsStoreFeatured == featured)
            return; // 冪等：狀態未變則不重複寫入 / 記錄。

        catalog.IsStoreFeatured = featured;

        // 新加入的精選接續排在現有精選之後；取消時不動排序值。
        if (featured)
        {
            var maxOrder = await db.Catalogs
                .Where(c => c.StoreId == catalog.StoreId && c.IsStoreFeatured && c.Id != catalog.Id)
                .MaxAsync(c => (int?)c.StoreFeaturedSortOrder, ct) ?? -1;
            catalog.StoreFeaturedSortOrder = maxOrder + 1;
        }

        auditLog.Add(currentUser.UserId, featured ? "catalog.store_feature" : "catalog.store_unfeature",
            "Catalog", catalog.Id, tenant: catalog.StoreId);

        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task ReorderStoreFeaturedAsync(ReorderStoreFeaturedRequest request, CancellationToken ct)
    {
        _ = currentUser.UserId ?? throw new UnauthorizedException();
        await storeClient.EnsureStoreOwnerAsync(request.StoreId, ct);

        // 該商店目前的店長精選商品全集。
        var featured = await db.Catalogs
            .Where(c => c.StoreId == request.StoreId && c.IsStoreFeatured)
            .ToListAsync(ct);

        var requestedIds = request.CatalogIds;

        // 請求須完整涵蓋現有精選集合（不多不少、不重複），避免遺漏商品排序未定義。
        if (requestedIds.Count != requestedIds.Distinct().Count())
            throw new ValidationException("店長精選商品 ID 不可重複。");

        var featuredIds = featured.Select(c => c.Id).ToHashSet();
        if (requestedIds.Count != featuredIds.Count || !requestedIds.All(featuredIds.Contains))
            throw new ValidationException("須完整涵蓋且僅包含目前的店長精選商品。");

        var byId = featured.ToDictionary(c => c.Id);
        for (var i = 0; i < requestedIds.Count; i++)
            byId[requestedIds[i]].StoreFeaturedSortOrder = i;

        auditLog.Add(currentUser.UserId, "catalog.store_feature.reorder",
            "Store", request.StoreId, tenant: request.StoreId);

        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<CatalogAssetUploadUrlResponse> RequestAssetUploadUrlAsync(
        Guid id, RequestCatalogAssetUploadUrlRequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException();
        var catalog = await LoadOwnedCatalogAsync(id, ct);

        var fileType = CatalogAssetContentTypes.Resolve(request.Type, request.ContentType);

        // 簽發階段不預扣配額、不建資產紀錄；使用者提交確認（confirm）時才建立 reference 並扣量。
        var result = await storageClient.RequestUploadUrlAsync(
            userId, catalog.Id, request.FileName, request.ContentType, request.SizeBytes,
            fileType, isPublic: true, ct);

        return new CatalogAssetUploadUrlResponse
        {
            AssetId = result.FileId,
            UploadUrl = result.UploadUrl,
            PublicUrl = result.PublicUrl ?? BuildPublicUrl(result.StorageKey),
            ExpiresAt = result.ExpiresAt,
        };
    }

    /// <inheritdoc/>
    public async Task<CatalogAssetDto> ConfirmAssetAsync(
        Guid id, Guid assetId, ConfirmCatalogAssetRequest request, CancellationToken ct)
    {
        var catalog = await LoadOwnedCatalogAsync(id, ct);

        // 1. 確認檔案已直傳完成並取得實際大小（冪等）。
        var file = await storageClient.ConfirmUploadAsync(assetId, ct);
        if (file.ProductId != catalog.Id)
            throw new ValidationException("檔案不屬於此商品。");
        if (file.Status != StorageFileStatus.Ready)
            throw new ValidationException("檔案尚未完成處理，請稍後再試。");

        // 2. 扣配額（冪等鍵 = 檔案 ID）；配額不足在建立 reference 前擋下。
        await quotaClient.ChargeAsync(assetId, file.SizeBytes ?? 0, catalog.Id, ct);

        // 3. 建立資產 reference（冪等：已存在則沿用）。
        var asset = await db.CatalogAssets
            .FirstOrDefaultAsync(a => a.Id == assetId && a.CatalogId == catalog.Id, ct);
        if (asset is null)
        {
            var nextSortOrder = await db.CatalogAssets
                .Where(a => a.CatalogId == catalog.Id && a.Type == request.Type)
                .CountAsync(ct);

            asset = new CatalogAsset
            {
                Id = assetId,
                CatalogId = catalog.Id,
                Type = request.Type,
                FileName = file.OriginalName,
                StorageKey = file.StorageKey,
                FileSize = file.SizeBytes ?? 0,
                ContentType = file.ContentType,
                SortOrder = nextSortOrder,
            };
            db.CatalogAssets.Add(asset);

            // 首張縮圖自動設為商品縮圖。
            if (request.Type == CatalogAssetType.Thumbnail && catalog.ThumbnailAssetId is null)
                catalog.ThumbnailAssetId = asset.Id;

            await db.SaveChangesAsync(ct);
        }

        // 4. 標記檔案已被使用（冪等），未標記檔案逾期會被 StorageService 清理且不計配額。
        await storageClient.MarkReferencedAsync(assetId, ct);

        var dto = mapper.Map<CatalogAssetDto>(asset);
        dto.Url = BuildPublicUrl(asset.StorageKey);
        return dto;
    }

    /// <inheritdoc/>
    public async Task DeleteAssetAsync(Guid id, Guid assetId, CancellationToken ct)
    {
        var catalog = await LoadOwnedCatalogAsync(id, ct);

        var asset = await db.CatalogAssets
            .FirstOrDefaultAsync(a => a.Id == assetId && a.CatalogId == catalog.Id, ct)
            ?? throw new NotFoundException("找不到資產。");

        // 先軟刪儲存端檔案（404 視為已刪，冪等），配額用量由 QuotaService 對帳釋放。
        await storageClient.DeleteFileAsync(assetId, ct);

        db.CatalogAssets.Remove(asset);

        if (catalog.ThumbnailAssetId == assetId)
            catalog.ThumbnailAssetId = null;

        await db.SaveChangesAsync(ct);
    }

    // ───────────────────────── 私有輔助 ─────────────────────────

    private Task<Catalog> LoadOwnedCatalogAsync(Guid id, CancellationToken ct) =>
        CatalogAuthorization.LoadOwnedCatalogAsync(db, storeClient, currentUser, id, ct);

    private async Task EnsureCategoryExistsAsync(Guid categoryId, CancellationToken ct)
    {
        var exists = await db.CatalogCategories.AnyAsync(c => c.Id == categoryId, ct);
        if (!exists)
            throw new ValidationException("指定的分類不存在。");
    }

    private static string NormalizeCurrency(string? currency)
    {
        var value = (currency ?? "TWD").Trim().ToUpperInvariant();
        if (value.Length != 3 || !value.All(char.IsLetter))
            throw new ValidationException("幣別須為 3 碼英文字母（ISO 4217）。");
        return value;
    }

    private static List<string> NormalizeTagNames(IEnumerable<string> tags) =>
        tags.Select(t => t.Trim().ToLowerInvariant())
            .Where(t => t.Length is > 0 and <= 50)
            .Distinct(StringComparer.Ordinal)
            .ToList();

    /// <summary>將商品標籤覆蓋為指定名稱集合，並維護標籤 UsageCount（不負責 SaveChanges）。</summary>
    private async Task ApplyTagsAsync(Guid catalogId, List<string> desiredNames, CancellationToken ct)
    {
        var existingMappings = await db.CatalogTagMappings
            .Where(m => m.CatalogId == catalogId)
            .ToListAsync(ct);

        var existingTagIds = existingMappings.Select(m => m.TagId).ToHashSet();

        // 解析 / 建立目標標籤
        var desiredTags = new List<CatalogTag>();
        foreach (var name in desiredNames)
        {
            var tag = await db.CatalogTags.FirstOrDefaultAsync(t => t.Name == name, ct);
            if (tag is null)
            {
                tag = new CatalogTag { Name = name, UsageCount = 0 };
                db.CatalogTags.Add(tag);
            }
            desiredTags.Add(tag);
        }

        var desiredTagIds = desiredTags.Select(t => t.Id).ToHashSet();

        // 移除不再需要的關聯
        foreach (var mapping in existingMappings.Where(m => !desiredTagIds.Contains(m.TagId)))
        {
            db.CatalogTagMappings.Remove(mapping);
            var tag = await db.CatalogTags.FirstOrDefaultAsync(t => t.Id == mapping.TagId, ct);
            if (tag is not null && tag.UsageCount > 0)
                tag.UsageCount--;
        }

        // 新增關聯
        foreach (var tag in desiredTags.Where(t => !existingTagIds.Contains(t.Id)))
        {
            db.CatalogTagMappings.Add(new CatalogTagMapping { CatalogId = catalogId, TagId = tag.Id });
            tag.UsageCount++;
        }
    }

    private async Task<CatalogDto> ToDtoAsync(Catalog catalog, CancellationToken ct)
    {
        var assets = await db.CatalogAssets.AsNoTracking()
            .Where(a => a.CatalogId == catalog.Id)
            .OrderBy(a => a.Type).ThenBy(a => a.SortOrder)
            .ToListAsync(ct);

        var tags = await db.CatalogTagMappings.AsNoTracking()
            .Where(m => m.CatalogId == catalog.Id)
            .Join(db.CatalogTags.AsNoTracking(), m => m.TagId, t => t.Id, (_, t) => t.Name)
            .OrderBy(name => name)
            .ToListAsync(ct);

        var dto = mapper.Map<CatalogDto>(catalog);
        dto.ThumbnailUrl = await GetAssetUrlAsync(catalog.ThumbnailAssetId, ct);
        dto.CurrentVersion = await GetCurrentVersionDtoAsync(catalog.CurrentVersionId, ct);
        dto.Assets = assets.Select(a =>
        {
            var assetDto = mapper.Map<CatalogAssetDto>(a);
            assetDto.Url = BuildPublicUrl(a.StorageKey);
            return assetDto;
        }).ToList();
        dto.Tags = tags;
        return dto;
    }

    private async Task<CatalogVersionDto?> GetCurrentVersionDtoAsync(Guid? versionId, CancellationToken ct)
    {
        if (versionId is null)
            return null;

        var version = await db.CatalogVersions.AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == versionId, ct);
        if (version is null)
            return null;

        var assets = await db.CatalogVersionAssets.AsNoTracking()
            .Where(a => a.CatalogVersionId == version.Id)
            .OrderBy(a => a.SortOrder)
            .ProjectTo<CatalogVersionAssetDto>(mapper.ConfigurationProvider)
            .ToListAsync(ct);

        var dto = mapper.Map<CatalogVersionDto>(version);
        dto.Assets = assets;
        return dto;
    }

    private async Task<string?> GetAssetUrlAsync(Guid? assetId, CancellationToken ct)
    {
        if (assetId is null)
            return null;

        var storageKey = await db.CatalogAssets.AsNoTracking()
            .Where(a => a.Id == assetId)
            .Select(a => a.StorageKey)
            .FirstOrDefaultAsync(ct);

        return storageKey is null ? null : BuildPublicUrl(storageKey);
    }

    private string BuildPublicUrl(string storageKey) => $"{_publicBaseUrl}/{storageKey}";
}
