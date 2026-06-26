using CatalogService.Models;

namespace CatalogService.Services.Reviews;

/// <summary>商品評論業務邏輯（評分 + 留言）。撰寫評論限已購買者；列表公開。</summary>
public interface ICatalogReviewService
{
    /// <summary>新增 / 更新目前使用者對某商品的評論（一人一商品一則）。須為已購買者。</summary>
    Task<CatalogReviewDto> UpsertAsync(Guid catalogId, UpsertReviewRequest request, CancellationToken ct);

    /// <summary>分頁列出某商品的評論（公開），含平均分與評論數。</summary>
    Task<ListReviewsResponse> ListAsync(Guid catalogId, ListReviewsRequest request, CancellationToken ct);

    /// <summary>取得目前使用者對某商品的評論；尚未評論則回傳 null。</summary>
    Task<CatalogReviewDto?> GetMineAsync(Guid catalogId, CancellationToken ct);

    /// <summary>刪除目前使用者對某商品的評論。</summary>
    Task DeleteMineAsync(Guid catalogId, CancellationToken ct);
}
