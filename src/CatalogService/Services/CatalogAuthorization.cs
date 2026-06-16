using CatalogService.Data;
using CatalogService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Auth;
using Shared.Exceptions;

namespace CatalogService.Services;

/// <summary>商品擁有權檢查共用邏輯（載入商品並確認目前使用者為所屬商店 Owner）。</summary>
public static class CatalogAuthorization
{
    /// <summary>載入商品並確認目前使用者為所屬商店 Owner，否則拋出對應例外。</summary>
    public static async Task<Catalog> LoadOwnedCatalogAsync(
        CatalogDbContext db, StoreServiceClient storeClient, ICurrentUserAccessor currentUser,
        Guid catalogId, CancellationToken ct)
    {
        _ = currentUser.UserId ?? throw new UnauthorizedException();

        var catalog = await db.Catalogs.FirstOrDefaultAsync(c => c.Id == catalogId, ct)
            ?? throw new NotFoundException("找不到商品。");

        await storeClient.EnsureStoreOwnerAsync(catalog.StoreId, ct);

        return catalog;
    }
}
