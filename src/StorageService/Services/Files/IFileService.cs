using StorageService.Models;

namespace StorageService.Services.Files;

/// <summary>數位商品檔案相關業務邏輯。</summary>
public interface IFileService
{
    /// <summary>建立檔案紀錄並產生上傳簽章 URL（presigned PUT）。</summary>
    Task<RequestUploadUrlResponse> RequestUploadUrlAsync(RequestUploadUrlRequest request, CancellationToken ct);

    /// <summary>查詢檔案元資訊與處理狀態。</summary>
    Task<FileDto> GetAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// 確認檔案已上傳至儲存後端並觸發處理 pipeline（標記 Ready + 發 FileReadyEvent）。
    /// 本地儲存由 blob 端點自動觸發，無需呼叫；雲端（GCS）由功能 API 在前端直傳成功後呼叫。
    /// 冪等：已處理（Processing/Ready/Failed）的檔案直接回傳現狀。
    /// </summary>
    Task<FileDto> ConfirmUploadAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// 標記檔案已被實際使用（功能 API 在使用者提交確認、建立資產 reference 後呼叫）。
    /// 僅 Ready 檔案可標記；冪等：已標記則直接回傳現狀。
    /// </summary>
    Task<FileDto> MarkReferencedAsync(Guid id, CancellationToken ct);

    /// <summary>取得 Ready 檔案的下載簽章 URL（presigned GET）。</summary>
    Task<GetDownloadUrlResponse> GetDownloadUrlAsync(Guid id, CancellationToken ct);

    /// <summary>軟刪除檔案。</summary>
    Task DeleteAsync(Guid id, CancellationToken ct);

    /// <summary>加總指定創作者已 Ready 且已被使用（referenced）檔案的位元組總和（QuotaService 每日對帳用）。</summary>
    Task<TenantUsageResponse> GetTenantUsageAsync(Guid creatorId, CancellationToken ct);

    /// <summary>彙總全平台儲存用量（數量 / 大小 / 公開私有拆分 / 孤兒檔 / 創作者明細）。Admin 用。</summary>
    Task<PlatformUsageResponse> GetPlatformUsageAsync(CancellationToken ct);
}
