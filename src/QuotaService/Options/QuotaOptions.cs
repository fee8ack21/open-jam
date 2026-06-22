namespace QuotaService.Options;

/// <summary>固定資源配額設定，對應 appsettings <c>Quota</c> 區段。MVP 不分方案，所有租戶共用此額度。</summary>
public class QuotaOptions
{
    /// <summary>帳號總儲存空間上限（bytes）。預設 50 GiB。</summary>
    public long MaxAccountStorageBytes { get; set; } = 53_687_091_200;

    /// <summary>單檔大小上限（bytes）。預設 2 GiB。</summary>
    public long MaxFileSizeBytes { get; set; } = 2_147_483_648;

    /// <summary>單一商品檔案總量上限（bytes）。預設 10 GiB。</summary>
    public long MaxProductTotalBytes { get; set; } = 10_737_418_240;

    /// <summary>上架（Published）商品數上限。預設 100。</summary>
    public int MaxPublishedProducts { get; set; } = 100;

    /// <summary>預扣有效期（分鐘）；須 ≥ signed URL 時效，並涵蓋大檔 resumable 上傳。預設 240。</summary>
    public int ReservationTtlMinutes { get; set; } = 240;
}
