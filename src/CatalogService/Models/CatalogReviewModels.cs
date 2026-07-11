namespace CatalogService.Models;

/// <summary>商品評論回應。</summary>
public class CatalogReviewDto
{
    /// <summary>評論唯一識別碼。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid Id { get; set; }

    /// <summary>所屬商品 ID。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid CatalogId { get; set; }

    /// <summary>評論者使用者 ID。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid ReviewerUserId { get; set; }

    /// <summary>評分（1–5）。</summary>
    /// <example>5</example>
    public int Rating { get; set; }

    /// <summary>留言內容；null 表示僅評分未留言。</summary>
    /// <example>非常實用，物超所值！</example>
    public string? Comment { get; set; }

    /// <summary>建立時間。</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>最後更新時間；null 表示未曾更新。</summary>
    public DateTimeOffset? UpdatedAt { get; set; }
}

/// <summary>新增 / 更新評論請求（同一使用者對同一商品為 upsert）。</summary>
public class UpsertReviewRequest
{
    /// <summary>評分（1–5）。</summary>
    /// <example>5</example>
    public int Rating { get; set; }

    /// <summary>留言內容（至多 2000 字）；null 或空字串表示僅評分。</summary>
    /// <example>非常實用，物超所值！</example>
    public string? Comment { get; set; }
}

/// <summary>商品評論列表查詢請求（分頁採 offset / limit）。</summary>
public class ListReviewsRequest
{
    /// <summary>略過筆數。</summary>
    /// <example>0</example>
    public int Offset { get; set; } = 0;

    /// <summary>每頁筆數（最大 100）。</summary>
    /// <example>20</example>
    public int Limit { get; set; } = 20;
}

/// <summary>商品評論列表分頁回應（含彙總）。</summary>
public class ListReviewsResponse
{
    /// <summary>平均評分（0–5）；無評論時為 0。</summary>
    /// <example>4.6</example>
    public double RatingAverage { get; set; }

    /// <summary>評論總數。</summary>
    /// <example>128</example>
    public int RatingCount { get; set; }

    /// <summary>
    /// 各星等評論數分佈；固定 5 個元素，索引 0 = 1★、索引 4 = 5★（供評分分佈長條圖）。
    /// </summary>
    /// <example>[3, 5, 12, 40, 68]</example>
    public int[] RatingDistribution { get; set; } = new int[5];

    /// <summary>本頁評論清單（依時間新到舊）。</summary>
    public List<CatalogReviewDto> Items { get; set; } = [];
}
