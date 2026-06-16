namespace CatalogService.Options;

/// <summary>物件儲存（資產公開讀取）相關設定，對應 appsettings <c>Storage</c> 區段。</summary>
public class StorageOptions
{
    /// <summary>公開讀取資產（縮圖 / 截圖 / 預覽影音）的 URL 前綴。</summary>
    /// <example>http://localhost:5171/v1/files/blob</example>
    public string PublicBaseUrl { get; set; } = null!;
}
