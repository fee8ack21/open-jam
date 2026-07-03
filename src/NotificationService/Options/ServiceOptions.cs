namespace NotificationService.Options;

/// <summary>外部微服務連線設定（設定區段 "Services"）。</summary>
public class ServiceOptions
{
    /// <summary>StoreService 連線設定。</summary>
    public ServiceEndpoint StoreService { get; set; } = new();

    /// <summary>單一服務端點。</summary>
    public class ServiceEndpoint
    {
        /// <summary>服務 Base URL。</summary>
        public string? BaseUrl { get; set; }
    }
}
