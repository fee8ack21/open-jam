namespace PaymentService.Options;

public class ServiceOptions
{
    public ServiceEndpointOptions CatalogService { get; set; } = new();
    public ServiceEndpointOptions StoreService { get; set; } = new();
}

public class ServiceEndpointOptions
{
    public string BaseUrl { get; set; } = null!;
}
