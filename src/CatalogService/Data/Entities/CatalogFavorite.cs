using Shared.Audit;

namespace CatalogService.Data.Entities;

/// <summary>使用者收藏的商品（wishlist）。複合主鍵 CatalogId + UserId，每位使用者對同一商品至多一筆。</summary>
public class CatalogFavorite : ICreatedAt
{
    /// <summary>被收藏的商品 ID。</summary>
    public Guid CatalogId { get; set; }

    /// <summary>收藏者使用者 ID（取自 JWT sub）。</summary>
    public Guid UserId { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; private set; }
}
