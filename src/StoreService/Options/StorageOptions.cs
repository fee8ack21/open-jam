namespace StoreService.Options;

/// <summary>物件儲存（資產公開讀取）相關設定，對應 appsettings <c>Storage</c> 區段。</summary>
public class StorageOptions
{
    /// <summary>公開讀取資產（Avatar/Banner）的 URL 前綴。</summary>
    /// <example>http://localhost:9000/open-jam</example>
    public string PublicBaseUrl { get; set; } = null!;
}
