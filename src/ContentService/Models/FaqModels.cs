namespace ContentService.Models;

/// <summary>常見問題列表查詢請求（管理員後台，分頁採 offset / limit）。</summary>
public class ListFaqItemsRequest
{
    /// <summary>略過筆數。</summary>
    /// <example>0</example>
    public int Offset { get; set; } = 0;

    /// <summary>每頁筆數（最大 100）。</summary>
    /// <example>20</example>
    public int Limit { get; set; } = 20;

    /// <summary>過濾主題分類 ID；null 表示不限。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid? CategoryId { get; set; }

    /// <summary>過濾發布狀態；null 表示不限。</summary>
    /// <example>true</example>
    public bool? IsPublished { get; set; }
}

/// <summary>常見問題項目。</summary>
public class FaqItemDto
{
    /// <summary>項目唯一識別碼。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid Id { get; set; }

    /// <summary>所屬主題分類 ID。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid CategoryId { get; set; }

    /// <summary>所屬主題分類名稱。</summary>
    /// <example>認識平台</example>
    public string CategoryName { get; set; } = "";

    /// <summary>所屬主題分類代稱。</summary>
    /// <example>platform</example>
    public string CategorySlug { get; set; } = "";

    /// <summary>問題。</summary>
    /// <example>Open Jam 是什麼？</example>
    public string Question { get; set; } = "";

    /// <summary>解答。</summary>
    /// <example>Open Jam 是台灣的數位商品平台。</example>
    public string Answer { get; set; } = "";

    /// <summary>同分類內的顯示排序（升冪）。</summary>
    /// <example>0</example>
    public int SortOrder { get; set; }

    /// <summary>是否已發布（對外公開）。</summary>
    /// <example>true</example>
    public bool IsPublished { get; set; }

    /// <summary>建立時間（UTC）。</summary>
    /// <example>2026-06-30T08:00:00Z</example>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>最後更新時間（UTC）；null 表示自建立後未變更。</summary>
    /// <example>2026-07-01T00:00:00Z</example>
    public DateTimeOffset? UpdatedAt { get; set; }
}

/// <summary>常見問題分頁查詢回應。</summary>
public class ListFaqItemsResponse
{
    /// <summary>符合條件的總筆數（未分頁）。</summary>
    /// <example>12</example>
    public int TotalCount { get; set; }

    /// <summary>本頁項目清單。</summary>
    public List<FaqItemDto> Items { get; set; } = [];
}

/// <summary>建立常見問題項目請求。</summary>
public class CreateFaqItemRequest
{
    /// <summary>所屬主題分類 ID。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid CategoryId { get; set; }

    /// <summary>問題。</summary>
    /// <example>Open Jam 是什麼？</example>
    public string Question { get; set; } = "";

    /// <summary>解答。</summary>
    /// <example>Open Jam 是台灣的數位商品平台。</example>
    public string Answer { get; set; } = "";

    /// <summary>同分類內的顯示排序（升冪）。</summary>
    /// <example>0</example>
    public int SortOrder { get; set; }

    /// <summary>是否已發布（對外公開）；預設為 true。</summary>
    /// <example>true</example>
    public bool IsPublished { get; set; } = true;
}

/// <summary>更新常見問題項目請求。</summary>
public class UpdateFaqItemRequest
{
    /// <summary>所屬主題分類 ID。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid CategoryId { get; set; }

    /// <summary>問題。</summary>
    /// <example>Open Jam 是什麼？</example>
    public string Question { get; set; } = "";

    /// <summary>解答。</summary>
    /// <example>Open Jam 是台灣的數位商品平台。</example>
    public string Answer { get; set; } = "";

    /// <summary>同分類內的顯示排序（升冪）。</summary>
    /// <example>0</example>
    public int SortOrder { get; set; }

    /// <summary>是否已發布（對外公開）。</summary>
    /// <example>true</example>
    public bool IsPublished { get; set; }
}
