using CatalogService.Models;
using CatalogService.Services;

namespace CatalogService.Services.CatalogVersions;

/// <summary>商品版本相關業務邏輯（版本管理與可下載檔案上傳 / 下載）。</summary>
public interface ICatalogVersionService
{
    /// <summary>列出商品的所有版本（新到舊）。僅 Owner 可操作。</summary>
    Task<List<CatalogVersionDto>> ListAsync(Guid catalogId, CancellationToken ct);

    /// <summary>建立新版本，並將其設為商品的目前版本。僅 Owner 可操作。</summary>
    Task<CatalogVersionDto> CreateAsync(Guid catalogId, CreateCatalogVersionRequest request, CancellationToken ct);

    /// <summary>申請版本可下載檔案上傳簽章 URL（私有物件）。簽發階段不扣配額。僅 Owner 可操作。</summary>
    Task<VersionAssetUploadUrlResponse> RequestAssetUploadUrlAsync(
        Guid catalogId, Guid versionId, RequestVersionAssetUploadUrlRequest request, CancellationToken ct);

    /// <summary>
    /// 確認版本可下載檔案上傳完成：扣配額、建立資產 reference 並標記檔案已使用。冪等。僅 Owner 可操作。
    /// </summary>
    Task<CatalogVersionAssetDto> ConfirmAssetAsync(
        Guid catalogId, Guid versionId, Guid assetId, CancellationToken ct);

    /// <summary>取得版本可下載檔案的下載簽章 URL（管理用途）。僅 Owner 可操作。</summary>
    Task<StorageDownloadUrlResult> GetAssetDownloadUrlAsync(
        Guid catalogId, Guid versionId, Guid assetId, CancellationToken ct);

    /// <summary>
    /// 列出買家已購商品某版本的可下載檔案（含短效下載 URL）。
    /// 以購買者身分驗證：須已有該商品的完成訂單，否則 403。
    /// </summary>
    Task<List<PurchasedVersionAssetDto>> ListPurchasedDownloadsAsync(
        Guid catalogId, Guid versionId, CancellationToken ct);

    /// <summary>刪除版本可下載檔案。僅 Owner 可操作。</summary>
    Task DeleteAssetAsync(Guid catalogId, Guid versionId, Guid assetId, CancellationToken ct);
}
