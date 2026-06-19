namespace CatalogService.Data.Entities;

/// <summary>商品分類，由平台維護。以 ParentId 自我參照實踐多層子分類。</summary>
public class CatalogCategory
{
    /// <summary>分類唯一識別碼。</summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>上層分類 ID；null 表示為頂層分類。</summary>
    public Guid? ParentId { get; set; }

    /// <summary>分類名稱。</summary>
    public string Name { get; set; } = "";

    /// <summary>分類代稱，全域唯一。</summary>
    public string Slug { get; set; } = "";

    /// <summary>分類補充敘述，前端用於分類卡片的說明文字；null 表示未設定。</summary>
    public string? Description { get; set; }

    /// <summary>同層分類的顯示排序（由小到大）。</summary>
    public int SortOrder { get; set; }
}
