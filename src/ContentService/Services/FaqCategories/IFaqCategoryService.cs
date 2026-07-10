using ContentService.Models;

namespace ContentService.Services.FaqCategories;

/// <summary>常見問題主題分類（FAQ Category）管理的業務邏輯（平台維護）。</summary>
public interface IFaqCategoryService
{
    /// <summary>列出所有分類（依排序、名稱）。匿名公開。</summary>
    Task<List<FaqCategoryDto>> ListAsync(CancellationToken ct = default);

    /// <summary>查詢單一分類。</summary>
    Task<FaqCategoryDto> GetAsync(Guid id, CancellationToken ct = default);

    /// <summary>建立分類。僅 Admin 可操作。</summary>
    Task<FaqCategoryDto> CreateAsync(CreateFaqCategoryRequest request, CancellationToken ct = default);

    /// <summary>更新分類。僅 Admin 可操作。</summary>
    Task<FaqCategoryDto> UpdateAsync(Guid id, UpdateFaqCategoryRequest request, CancellationToken ct = default);

    /// <summary>刪除分類（不可仍被常見問題項目引用）。僅 Admin 可操作。</summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
