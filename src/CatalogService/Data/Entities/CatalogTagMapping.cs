namespace CatalogService.Data.Entities;

/// <summary>商品與標籤的多對多關聯（複合主鍵 CatalogId + TagId）。</summary>
public class CatalogTagMapping
{
    /// <summary>商品 ID。</summary>
    public Guid CatalogId { get; set; }

    /// <summary>標籤 ID。</summary>
    public Guid TagId { get; set; }
}
