namespace CatalogService.Data.Entities;

/// <summary>商品標籤。名稱強制小寫並全域唯一，於商品掛載 / 卸載時維護使用次數。</summary>
public class CatalogTag
{
    /// <summary>標籤唯一識別碼。</summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>標籤名稱，強制小寫，全域唯一。</summary>
    public string Name { get; set; } = "";

    /// <summary>被商品引用的次數。</summary>
    public int UsageCount { get; set; }
}
