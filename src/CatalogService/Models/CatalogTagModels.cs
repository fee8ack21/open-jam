namespace CatalogService.Models;

/// <summary>商品標籤回應。</summary>
public class CatalogTagDto
{
    /// <summary>標籤唯一識別碼。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid Id { get; set; }

    /// <summary>標籤名稱（小寫）。</summary>
    /// <example>retro</example>
    public string Name { get; set; } = "";

    /// <summary>被商品引用的次數。</summary>
    /// <example>42</example>
    public int UsageCount { get; set; }
}

/// <summary>商品標籤查詢請求（分頁採 offset / limit，依使用次數遞減）。</summary>
public class ListCatalogTagsRequest
{
    /// <summary>名稱前綴關鍵字（強制小寫）；null 表示不限。</summary>
    /// <example>ret</example>
    public string? Search { get; set; }

    /// <summary>略過筆數。</summary>
    /// <example>0</example>
    public int Offset { get; set; } = 0;

    /// <summary>每頁筆數（最大 100）。</summary>
    /// <example>20</example>
    public int Limit { get; set; } = 20;
}

/// <summary>商品標籤分頁回應。</summary>
public class ListCatalogTagsResponse
{
    /// <summary>符合條件的總筆數（未分頁）。</summary>
    /// <example>128</example>
    public int TotalCount { get; set; }

    /// <summary>本頁標籤清單。</summary>
    public List<CatalogTagDto> Items { get; set; } = [];
}

/// <summary>熱門標籤查詢請求（依已上架商品引用數遞減取前 N）。</summary>
public class PopularCatalogTagsRequest
{
    /// <summary>取回筆數（1–50，超出範圍自動夾限）。</summary>
    /// <example>14</example>
    public int Limit { get; set; } = 14;
}

/// <summary>熱門標籤回應（供市集跑馬燈等公開版位）。</summary>
public class PopularCatalogTagsResponse
{
    /// <summary>熱門標籤清單（依已上架商品引用數遞減）。</summary>
    public List<CatalogTagDto> Items { get; set; } = [];
}
