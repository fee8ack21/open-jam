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

    /// <summary>Pending 訂單逾期時數，超過即由清理排程自動取消。須大於 Stripe Checkout Session
    /// 的 24 小時存活上限，確保清理時 Session 必已自然過期。</summary>
    /// <example>25</example>
    public int PendingExpiryHours { get; set; } = 25;

    /// <summary>清理排程單輪處理的訂單數上限。</summary>
    /// <example>50</example>
    public int CleanupBatchSize { get; set; } = 50;
}
