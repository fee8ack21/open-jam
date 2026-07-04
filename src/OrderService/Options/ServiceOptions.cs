namespace OrderService.Options;

/// <summary>對外部微服務端點的設定，對應 appsettings <c>Services</c> 區段。</summary>
public class ServiceOptions
{
    /// <summary>StoreService（商店 / 成員 API）端點設定。</summary>
    public ServiceEndpointOptions StoreService { get; set; } = new();

    /// <summary>PaymentService（金流 / Checkout Session API）端點設定。</summary>
    public ServiceEndpointOptions PaymentService { get; set; } = new();
}

/// <summary>單一外部服務端點設定。</summary>
public class ServiceEndpointOptions
{
    /// <summary>服務根 URL。</summary>
    /// <example>http://localhost:5172</example>
    public string BaseUrl { get; set; } = null!;
}
