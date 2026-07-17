using Shared.Audit;

namespace CatalogService.Data.Entities;

/// <summary>
/// 商品評論（評分 + 選填留言）。同一評論者對同一商品至多一則，可更新。
/// 評論者身分為二擇一：登入買家以 <see cref="ReviewerUserId"/> 識別；未註冊訪客憑訂單 ID
/// 下單，以下單信箱 <see cref="ReviewerEmail"/> 識別。
/// </summary>
public class CatalogReview : ICreatedAt, IUpdatedAt
{
    /// <summary>評論唯一識別碼。</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>所屬商品 ID。</summary>
    public Guid CatalogId { get; set; }

    /// <summary>登入評論者使用者 ID（須為已購買者）；訪客評論為 null。</summary>
    public Guid? ReviewerUserId { get; set; }

    /// <summary>訪客評論者下單信箱（一律小寫，須為該商品已完成訂單的買家）；登入評論為 null。</summary>
    public string? ReviewerEmail { get; set; }

    /// <summary>評分（1–5）。</summary>
    public int Rating { get; set; }

    /// <summary>留言內容；null 表示僅評分未留言。</summary>
    public string? Comment { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <inheritdoc/>
    public DateTimeOffset? UpdatedAt { get; private set; }
}
