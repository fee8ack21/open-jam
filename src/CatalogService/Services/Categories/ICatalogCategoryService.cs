using CatalogService.Models;

namespace CatalogService.Services.Categories;

/// <summary>商品分類業務邏輯（平台維護）。</summary>
public interface ICatalogCategoryService
{
    /// <summary>列出所有分類（依上層、排序）。公開。</summary>
    Task<List<CatalogCategoryDto>> ListAsync(CancellationToken ct);

    /// <summary>查詢單一分類。公開。</summary>
    Task<CatalogCategoryDto> GetAsync(Guid id, CancellationToken ct);

    /// <summary>建立分類。僅 Admin 可操作。</summary>
    Task<CatalogCategoryDto> CreateAsync(CreateCatalogCategoryRequest request, CancellationToken ct);

    /// <summary>更新分類。僅 Admin 可操作。</summary>
    Task<CatalogCategoryDto> UpdateAsync(Guid id, UpdateCatalogCategoryRequest request, CancellationToken ct);

    /// <summary>刪除分類（不可有子分類或被商品引用）。僅 Admin 可操作。</summary>
    Task DeleteAsync(Guid id, CancellationToken ct);
}
