using Shared.Audit;

namespace CatalogService.Data.Entities;

/// <summary>商品評論（評分 + 選填留言）。同一使用者對同一商品至多一則，可更新。</summary>
public class CatalogReview : ICreatedAt, IUpdatedAt
{
    /// <summary>評論唯一識別碼。</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>所屬商品 ID。</summary>
    public Guid CatalogId { get; set; }

    /// <summary>評論者使用者 ID（須為已購買者）。</summary>
    public Guid ReviewerUserId { get; set; }

    /// <summary>評分（1–5）。</summary>
    public int Rating { get; set; }

    /// <summary>留言內容；null 表示僅評分未留言。</summary>
    public string? Comment { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <inheritdoc/>
    public DateTimeOffset? UpdatedAt { get; private set; }
}
