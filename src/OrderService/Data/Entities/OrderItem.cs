namespace OrderService.Data.Entities;

/// <summary>訂單項目，對應一筆購買的商品版本。數位商品數量固定為 1。</summary>
public class OrderItem
{
    public Guid Id { get; set; }

    /// <summary>所屬訂單 ID。</summary>
    public Guid OrderId { get; set; }

    /// <summary>商品 ID（由 CatalogService 產生）。</summary>
    public Guid CatalogId { get; set; }

    /// <summary>購買的商品版本 ID（由 CatalogService 產生）。</summary>
    public Guid CatalogVersionId { get; set; }

    /// <summary>下單當下的商品名稱快照（商品改名後仍保留歷史名稱）。</summary>
    public string CatalogName { get; set; } = "";

    /// <summary>單價（最低貨幣單位，如 cents）。</summary>
    public long UnitPrice { get; set; }
}
