using CatalogService.Data;
using CatalogService.Data.Entities;
using CatalogService.Models;
using Microsoft.EntityFrameworkCore;
using Shared.Auth;
using Shared.Exceptions;

namespace CatalogService.Services.Favorites;

/// <summary>使用者商品收藏（wishlist）業務邏輯實作。收藏為登入使用者所有（UserId 取自 JWT sub）。</summary>
public class CatalogFavoriteService(CatalogDbContext db, ICurrentUserAccessor currentUser) : ICatalogFavoriteService
{
    /// <inheritdoc/>
    public async Task AddAsync(Guid catalogId, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException();

        var catalogExists = await db.Catalogs.AnyAsync(c => c.Id == catalogId, ct);
        if (!catalogExists)
            throw new NotFoundException("找不到商品。");

        var alreadyFavorited = await db.CatalogFavorites
            .AnyAsync(f => f.CatalogId == catalogId && f.UserId == userId, ct);

        if (!alreadyFavorited)
        {
            db.CatalogFavorites.Add(new CatalogFavorite
            {
                CatalogId = catalogId,
                UserId = userId,
            });

            await db.SaveChangesAsync(ct);
        }
    }

    /// <inheritdoc/>
    public async Task RemoveAsync(Guid catalogId, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException();

        var favorite = await db.CatalogFavorites
            .FirstOrDefaultAsync(f => f.CatalogId == catalogId && f.UserId == userId, ct);

        if (favorite is not null)
        {
            db.CatalogFavorites.Remove(favorite);
            await db.SaveChangesAsync(ct);
        }
    }

    /// <inheritdoc/>
    public async Task<CatalogFavoritesResponse> ListMineAsync(CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException();

        var catalogIds = await db.CatalogFavorites.AsNoTracking()
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => f.CatalogId)
            .ToListAsync(ct);

        return new CatalogFavoritesResponse { CatalogIds = catalogIds };
    }
}
