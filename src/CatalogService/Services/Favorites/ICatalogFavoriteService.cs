using CatalogService.Models;

namespace CatalogService.Services.Favorites;

/// <summary>使用者商品收藏（wishlist）相關業務邏輯。</summary>
public interface ICatalogFavoriteService
{
    /// <summary>收藏商品。已收藏則 no-op。</summary>
    Task AddAsync(Guid catalogId, CancellationToken ct);

    /// <summary>取消收藏商品。未收藏則 no-op。</summary>
    Task RemoveAsync(Guid catalogId, CancellationToken ct);

    /// <summary>查詢目前使用者已收藏的商品 ID 清單。</summary>
    Task<CatalogFavoritesResponse> ListMineAsync(CancellationToken ct);
}
