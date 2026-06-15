namespace StorageService.Services.StorageEvents;

/// <summary>處理儲存後端（本地檔案 / GCS）物件事件通知的業務邏輯。</summary>
public interface IStorageEventService
{
    /// <summary>
    /// 處理 ObjectCreated 事件：依物件鍵值定位檔案、更新大小並觸發處理 pipeline。
    /// </summary>
    /// <param name="objectKey">物件鍵值（不含 bucket 名稱），格式 creators/{creatorId}/{fileId}/{originalName}。</param>
    /// <param name="reportedSize">儲存後端回報的物件大小（bytes），可為 null。</param>
    /// <param name="ct">Cancellation token。</param>
    /// <returns>鍵值格式可辨識並已受理回傳 true；無法解析鍵值回傳 false。</returns>
    Task<bool> HandleObjectCreatedAsync(string objectKey, long? reportedSize, CancellationToken ct);
}
