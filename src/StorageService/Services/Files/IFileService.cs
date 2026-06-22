using StorageService.Models;

namespace StorageService.Services.Files;

/// <summary>數位商品檔案相關業務邏輯。</summary>
public interface IFileService
{
    /// <summary>建立檔案紀錄並產生上傳簽章 URL（presigned PUT）。</summary>
    Task<RequestUploadUrlResponse> RequestUploadUrlAsync(RequestUploadUrlRequest request, CancellationToken ct);

    /// <summary>查詢檔案元資訊與處理狀態。</summary>
    Task<FileDto> GetAsync(Guid id, CancellationToken ct);

    /// <summary>取得 Ready 檔案的下載簽章 URL（presigned GET）。</summary>
    Task<GetDownloadUrlResponse> GetDownloadUrlAsync(Guid id, CancellationToken ct);

    /// <summary>軟刪除檔案。</summary>
    Task DeleteAsync(Guid id, CancellationToken ct);

    /// <summary>加總指定創作者已 Ready 檔案的位元組總和（QuotaService 每日對帳用）。</summary>
    Task<TenantUsageResponse> GetTenantUsageAsync(Guid creatorId, CancellationToken ct);
}
