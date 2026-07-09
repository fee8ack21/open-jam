using ContentService.Data.Entities;
using ContentService.Models;

namespace ContentService.Services.Faqs;

/// <summary>常見問題（FAQ）管理的業務邏輯。</summary>
public interface IFaqService
{
    /// <summary>分頁查詢常見問題（管理員後台列表）。</summary>
    Task<ListFaqItemsResponse> ListAsync(ListFaqItemsRequest request, CancellationToken ct = default);

    /// <summary>取得已發布的常見問題（匿名公開，依分類與排序）；category 為 null 時回傳所有分類。</summary>
    Task<List<FaqItemDto>> GetPublishedAsync(FaqCategory? category, CancellationToken ct = default);

    /// <summary>取得單筆常見問題。</summary>
    Task<FaqItemDto> GetAsync(Guid id, CancellationToken ct = default);

    /// <summary>建立常見問題項目。</summary>
    Task<FaqItemDto> CreateAsync(CreateFaqItemRequest request, CancellationToken ct = default);

    /// <summary>更新常見問題項目。</summary>
    Task<FaqItemDto> UpdateAsync(Guid id, UpdateFaqItemRequest request, CancellationToken ct = default);

    /// <summary>刪除常見問題項目。</summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
