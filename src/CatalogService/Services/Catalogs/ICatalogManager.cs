using CatalogService.Models;

namespace CatalogService.Services.Catalogs;

/// <summary>商品相關業務邏輯（建立、查詢、更新、狀態管理、標籤 / 分類、展示型資產上傳）。</summary>
public interface ICatalogManager
{
    /// <summary>建立商品（草稿）。僅商店 Owner 可操作。</summary>
    Task<CatalogDto> CreateAsync(CreateCatalogRequest request, CancellationToken ct);

    /// <summary>查詢商品完整資訊。未上架商品僅 Owner 可見。</summary>
    Task<CatalogDto> GetAsync(Guid id, CancellationToken ct);

    /// <summary>分頁查詢商品列表。<paramref name="publishedOnly"/> 為 true 時僅回傳已上架商品。</summary>
    Task<ListCatalogsResponse> ListAsync(ListCatalogsRequest request, bool publishedOnly, CancellationToken ct);

    /// <summary>更新商品基本資料。僅 Owner 可操作。</summary>
    Task<CatalogDto> UpdateAsync(Guid id, UpdateCatalogRequest request, CancellationToken ct);

    /// <summary>設定 / 移除商品分類。僅 Owner 可操作。</summary>
    Task<CatalogDto> SetCategoryAsync(Guid id, SetCatalogCategoryRequest request, CancellationToken ct);

    /// <summary>全量覆蓋商品標籤，回傳最終標籤名稱清單。僅 Owner 可操作。</summary>
    Task<List<string>> SetTagsAsync(Guid id, SetCatalogTagsRequest request, CancellationToken ct);

    /// <summary>上架商品（Draft/Archived → Published）。需已有目前版本。僅 Owner 可操作。</summary>
    Task PublishAsync(Guid id, CancellationToken ct);

    /// <summary>下架封存商品（Published → Archived）。僅 Owner 可操作。</summary>
    Task ArchiveAsync(Guid id, CancellationToken ct);

    /// <summary>平台停權商品（任意狀態 → Suspended）。僅 Admin 可操作。</summary>
    Task SuspendAsync(Guid id, CancellationToken ct);

    /// <summary>解除商品停權（Suspended → Archived，需 Owner 重新上架）。僅 Admin 可操作。</summary>
    Task UnsuspendAsync(Guid id, CancellationToken ct);

    /// <summary>申請展示型資產（縮圖 / 截圖 / 預覽影音）上傳簽章 URL。僅 Owner 可操作。</summary>
    Task<CatalogAssetUploadUrlResponse> RequestAssetUploadUrlAsync(
        Guid id, RequestCatalogAssetUploadUrlRequest request, CancellationToken ct);

    /// <summary>刪除展示型資產。僅 Owner 可操作。</summary>
    Task DeleteAssetAsync(Guid id, Guid assetId, CancellationToken ct);
}
