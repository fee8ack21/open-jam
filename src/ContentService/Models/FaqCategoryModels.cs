namespace ContentService.Models;

/// <summary>常見問題主題分類回應。</summary>
public class FaqCategoryDto
{
    /// <summary>分類唯一識別碼。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid Id { get; set; }

    /// <summary>分類名稱。</summary>
    /// <example>認識平台</example>
    public string Name { get; set; } = "";

    /// <summary>分類代稱（全域唯一）。</summary>
    /// <example>platform</example>
    public string Slug { get; set; } = "";

    /// <summary>分類補充敘述；null 表示未設定。</summary>
    /// <example>關於 Open Jam 平台的基本介紹</example>
    public string? Description { get; set; }

    /// <summary>分頁顯示排序。</summary>
    /// <example>0</example>
    public int SortOrder { get; set; }
}

/// <summary>建立常見問題主題分類請求（平台維護）。</summary>
public class CreateFaqCategoryRequest
{
    /// <summary>分類名稱（1–100 字）。</summary>
    /// <example>認識平台</example>
    public string Name { get; set; } = "";

    /// <summary>分類代稱（全域唯一，3–100 字小寫英數字與連字號）。</summary>
    /// <example>platform</example>
    public string Slug { get; set; } = "";

    /// <summary>分類補充敘述（選填，最多 200 字）。</summary>
    /// <example>關於 Open Jam 平台的基本介紹</example>
    public string? Description { get; set; }

    /// <summary>分頁顯示排序。</summary>
    /// <example>0</example>
    public int SortOrder { get; set; }
}

/// <summary>更新常見問題主題分類請求（部分欄位，null 表示不變更）。</summary>
public class UpdateFaqCategoryRequest
{
    /// <summary>分類名稱；null 表示不變更。</summary>
    /// <example>認識平台</example>
    public string? Name { get; set; }

    /// <summary>分類代稱；null 表示不變更。</summary>
    /// <example>platform</example>
    public string? Slug { get; set; }

    /// <summary>分類補充敘述；null 表示不變更。</summary>
    /// <example>關於 Open Jam 平台的基本介紹</example>
    public string? Description { get; set; }

    /// <summary>分頁顯示排序；null 表示不變更。</summary>
    /// <example>1</example>
    public int? SortOrder { get; set; }
}
