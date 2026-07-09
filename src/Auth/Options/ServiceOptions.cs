namespace Auth.Options;

/// <summary>對外部微服務端點的設定，對應 appsettings <c>Services</c> 區段。</summary>
public class ServiceOptions
{
    /// <summary>ContentService（法律文件 / FAQ 內容 API）端點設定。</summary>
    public ServiceEndpointOptions ContentService { get; set; } = new();
}

/// <summary>單一外部服務端點設定。</summary>
public class ServiceEndpointOptions
{
    /// <summary>服務根 URL。</summary>
    /// <example>http://localhost:5181</example>
    public string BaseUrl { get; set; } = null!;
}
