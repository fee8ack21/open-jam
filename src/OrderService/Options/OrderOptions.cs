namespace OrderService.Options;

/// <summary>訂單相關設定，對應 appsettings <c>Order</c> 區段。</summary>
public class OrderOptions
{
    /// <summary>
    /// 訂單完成信中的下載頁 URL 樣板，支援 <c>{storeSlug}</c> / <c>{orderId}</c> 佔位符。
    /// 開發環境覆蓋為 creator-web dev server（無子網域）。
    /// </summary>
    /// <example>https://{storeSlug}.openjam.co/orders/{orderId}</example>
    public string DownloadUrlPattern { get; set; } = "https://{storeSlug}.openjam.co/orders/{orderId}";
}
