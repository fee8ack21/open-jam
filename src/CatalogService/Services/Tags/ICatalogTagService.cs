using CatalogService.Models;

namespace CatalogService.Services.Tags;

/// <summary>商品標籤業務邏輯（瀏覽 / 搜尋）。</summary>
public interface ICatalogTagService
{
    /// <summary>分頁查詢標籤（依使用次數遞減）。公開。</summary>
    Task<ListCatalogTagsResponse> ListAsync(ListCatalogTagsRequest request, CancellationToken ct);
}
