namespace NotificationService.Options;

/// <summary>通知發送行為設定（設定區段 "Notification"）。</summary>
public class NotificationOptions
{
    /// <summary>
    /// 商品頁網址樣板，佔位符 {storeSlug} / {catalogId}。
    /// 正式為 https://{storeSlug}.openjam.co/product/{catalogId}，地端開發指向 creator-web dev server。
    /// </summary>
    public string CatalogUrlPattern { get; set; } = "https://{storeSlug}.openjam.co/product/{catalogId}";

    /// <summary>
    /// 買家訂單下載頁網址樣板，佔位符 {storeSlug} / {orderId}。
    /// 正式為 https://{storeSlug}.openjam.co/orders/{orderId}，地端開發指向 creator-web dev server。
    /// catalog.version_released 通知信以買家自己的完成訂單 ID 組出下載頁連結。
    /// </summary>
    public string OrderUrlPattern { get; set; } = "https://{storeSlug}.openjam.co/orders/{orderId}";

    /// <summary>單一通知任務發送嘗試上限，達上限轉為 Failed。</summary>
    public int DispatchMaxAttempts { get; set; } = 5;

    /// <summary>fan-out 讀取追蹤者的分批筆數。</summary>
    public int DispatchBatchSize { get; set; } = 500;
}
