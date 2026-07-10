namespace ContentService.Data.Entities;

/// <summary>
/// 常見問題主題分類（對應 portal-web FAQ 頁的主題分頁），由平台維護、可 CRUD。
/// 分類為單層（無子分類）；以 <see cref="SortOrder"/> 決定分頁顯示順序。
/// </summary>
public class FaqCategory
{
    /// <summary>分類唯一識別碼。</summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>分類名稱（顯示於 FAQ 頁分頁與問答標籤）。</summary>
    public string Name { get; set; } = "";

    /// <summary>分類代稱，全域唯一（小寫英數字與連字號）。</summary>
    public string Slug { get; set; } = "";

    /// <summary>分類補充敘述；null 表示未設定。</summary>
    public string? Description { get; set; }

    /// <summary>分頁顯示排序（由小到大）。</summary>
    public int SortOrder { get; set; }

    /// <summary>屬於此分類的常見問題項目。</summary>
    public ICollection<FaqItem> Items { get; set; } = [];
}
