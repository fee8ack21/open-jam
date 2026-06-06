namespace StorageService.Storage;

/// <summary>
/// 物件儲存後端抽象介面。地端使用 MinIO 實作，雲端切換為 Google Cloud Storage 實作，
/// 業務邏輯不依賴具體後端。
/// </summary>
public interface IStorageProvider
{
    /// <summary>
    /// 簽發上傳用 presigned PUT URL，前端透過此 URL 直傳檔案，不經 API 轉傳。
    /// </summary>
    /// <param name="key">物件鍵值（路徑），例如 "creators/{id}/{fileId}/photo.jpg"。</param>
    /// <param name="contentType">預期的 MIME 類型，用於 Content-Type 限制。</param>
    /// <param name="maxBytes">允許上傳的最大位元組數。</param>
    /// <param name="expiry">簽章有效時限。</param>
    /// <param name="ct">Cancellation token。</param>
    Task<string> GenerateUploadUrlAsync(string key, string contentType, long maxBytes, TimeSpan expiry, CancellationToken ct = default);

    /// <summary>簽發下載用 presigned GET URL，用於一般檔案（非 HLS 串流）授權下載。</summary>
    /// <param name="key">物件鍵值。</param>
    /// <param name="expiry">簽章有效時限。</param>
    /// <param name="ct">Cancellation token。</param>
    Task<string> GenerateDownloadUrlAsync(string key, TimeSpan expiry, CancellationToken ct = default);

    /// <summary>從儲存後端永久刪除物件（用於孤兒清理或硬刪除）。</summary>
    Task DeleteAsync(string key, CancellationToken ct = default);

    /// <summary>確認物件是否存在於儲存後端（用於上傳狀態驗證）。</summary>
    Task<bool> ObjectExistsAsync(string key, CancellationToken ct = default);
}
