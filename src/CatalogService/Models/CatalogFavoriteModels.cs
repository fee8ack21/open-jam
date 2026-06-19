namespace CatalogService.Models;

/// <summary>目前使用者的商品收藏（wishlist）回應。</summary>
public class CatalogFavoritesResponse
{
    /// <summary>目前使用者已收藏的商品 ID 清單（依收藏時間遞減）。</summary>
    public List<Guid> CatalogIds { get; set; } = [];
}
