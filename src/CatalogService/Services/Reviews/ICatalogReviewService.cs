using CatalogService.Models;

namespace CatalogService.Services.Reviews;

/// <summary>
/// 商品評論業務邏輯（評分 + 留言）。撰寫評論限已購買者；列表公開。
/// 評論者身分二擇一：登入買家由 JWT 帶入身分；未註冊訪客傳入 <c>orderId</c>，以下單信箱識別。
/// </summary>
public interface ICatalogReviewService
{
    /// <summary>新增 / 更新評論（一人一商品一則）。登入買家以 JWT 身分；訪客須傳 <paramref name="orderId"/>。</summary>
    Task<CatalogReviewDto> UpsertAsync(Guid catalogId, UpsertReviewRequest request, Guid? orderId, CancellationToken ct);

    /// <summary>分頁列出某商品的評論（公開），含平均分與評論數。</summary>
    Task<ListReviewsResponse> ListAsync(Guid catalogId, ListReviewsRequest request, CancellationToken ct);

    /// <summary>取得評論者對某商品的評論；尚未評論則回傳 null。訪客須傳 <paramref name="orderId"/>。</summary>
    Task<CatalogReviewDto?> GetMineAsync(Guid catalogId, Guid? orderId, CancellationToken ct);

    /// <summary>刪除評論者對某商品的評論。訪客須傳 <paramref name="orderId"/>。</summary>
    Task DeleteMineAsync(Guid catalogId, Guid? orderId, CancellationToken ct);
}
