namespace CatalogService.Options;

/// <summary>對外部微服務端點的設定，對應 appsettings <c>Services</c> 區段。</summary>
public class ServiceOptions
{
    /// <summary>StorageService（物件儲存 API）端點設定。</summary>
    public ServiceEndpointOptions StorageService { get; set; } = new();

    /// <summary>StoreService（商店 / 成員 API）端點設定。</summary>
    public ServiceEndpointOptions StoreService { get; set; } = new();

    /// <summary>QuotaService（資源配額 API）端點設定。</summary>
    public ServiceEndpointOptions QuotaService { get; set; } = new();

    /// <summary>OrderService（訂單 API，用於評論購買驗證）端點設定。</summary>
    public ServiceEndpointOptions OrderService { get; set; } = new();

    /// <summary>PaymentService（金流 API，用於付費商品上架的收款狀態閘門）端點設定。</summary>
    public ServiceEndpointOptions PaymentService { get; set; } = new();
}

/// <summary>單一外部服務端點設定。</summary>
public class ServiceEndpointOptions
{
    /// <summary>服務根 URL。</summary>
    /// <example>http://localhost:5171</example>
    public string BaseUrl { get; set; } = null!;
}
