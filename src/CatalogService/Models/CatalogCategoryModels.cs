namespace CatalogService.Models;

/// <summary>商品分類回應。</summary>
public class CatalogCategoryDto
{
    /// <summary>分類唯一識別碼。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid Id { get; set; }

    /// <summary>上層分類 ID；null 表示頂層分類。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid? ParentId { get; set; }

    /// <summary>分類名稱。</summary>
    /// <example>音樂與音效</example>
    public string Name { get; set; } = "";

    /// <summary>分類代稱（全域唯一）。</summary>
    /// <example>audio</example>
    public string Slug { get; set; } = "";

    /// <summary>分類補充敘述；null 表示未設定。</summary>
    /// <example>樂譜、配樂、分軌音檔</example>
    public string? Description { get; set; }

    /// <summary>同層顯示排序。</summary>
    /// <example>0</example>
    public int SortOrder { get; set; }

    /// <summary>是否為系統預設分類；系統預設分類不允許刪除。</summary>
    /// <example>false</example>
    public bool IsSystem { get; set; }
}

/// <summary>建立商品分類請求（平台維護）。</summary>
public class CreateCatalogCategoryRequest
{
    /// <summary>上層分類 ID；null 表示頂層分類。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid? ParentId { get; set; }

    /// <summary>分類名稱（1–100 字）。</summary>
    /// <example>音樂與音效</example>
    public string Name { get; set; } = "";

    /// <summary>分類代稱（全域唯一，3–100 字小寫英數字與連字號）。</summary>
    /// <example>audio</example>
    public string Slug { get; set; } = "";

    /// <summary>分類補充敘述（選填，最多 200 字）。</summary>
    /// <example>樂譜、配樂、分軌音檔</example>
    public string? Description { get; set; }

    /// <summary>同層顯示排序。</summary>
    /// <example>0</example>
    public int SortOrder { get; set; }
}

/// <summary>更新商品分類請求（部分欄位，null 表示不變更）。</summary>
public class UpdateCatalogCategoryRequest
{
    /// <summary>上層分類 ID；未提供（欄位缺省）表示不變更。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid? ParentId { get; set; }

    /// <summary>分類名稱；null 表示不變更。</summary>
    /// <example>音樂與音效</example>
    public string? Name { get; set; }

    /// <summary>分類代稱；null 表示不變更。</summary>
    /// <example>audio</example>
    public string? Slug { get; set; }

    /// <summary>分類補充敘述；null 表示不變更。</summary>
    /// <example>樂譜、配樂、分軌音檔</example>
    public string? Description { get; set; }

    /// <summary>同層顯示排序；null 表示不變更。</summary>
    /// <example>1</example>
    public int? SortOrder { get; set; }
}
