using CatalogService.Models;

namespace CatalogService.Services.Tags;

/// <summary>商品標籤業務邏輯（瀏覽 / 搜尋）。</summary>
public interface ICatalogTagService
{
    /// <summary>分頁查詢標籤（依使用次數遞減）。公開。</summary>
    Task<ListCatalogTagsResponse> ListAsync(ListCatalogTagsRequest request, CancellationToken ct);

    /// <summary>取熱門標籤（僅計已上架商品引用數，依次數遞減取前 N）。公開。</summary>
    Task<PopularCatalogTagsResponse> ListPopularAsync(PopularCatalogTagsRequest request, CancellationToken ct);
}
