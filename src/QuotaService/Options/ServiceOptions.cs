namespace QuotaService.Options;

/// <summary>對外部微服務端點的設定，對應 appsettings <c>Services</c> 區段。</summary>
public class ServiceOptions
{
    /// <summary>StorageService（物件儲存 API）端點設定，供每日對帳加總實際用量。</summary>
    public ServiceEndpointOptions StorageService { get; set; } = new();
}

/// <summary>單一外部服務端點設定。</summary>
public class ServiceEndpointOptions
{
    /// <summary>服務根 URL。</summary>
    /// <example>http://localhost:5171</example>
    public string BaseUrl { get; set; } = null!;
}
