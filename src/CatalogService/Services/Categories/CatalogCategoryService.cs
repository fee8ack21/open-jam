using AutoMapper;
using AutoMapper.QueryableExtensions;
using CatalogService.Data;
using CatalogService.Data.Entities;
using CatalogService.Models;
using Microsoft.EntityFrameworkCore;
using Shared.Exceptions;

namespace CatalogService.Services.Categories;

/// <summary>商品分類業務邏輯實作。</summary>
public class CatalogCategoryService(CatalogDbContext db, IMapper mapper) : ICatalogCategoryService
{
    /// <inheritdoc/>
    public async Task<List<CatalogCategoryDto>> ListAsync(CancellationToken ct) =>
        await db.CatalogCategories.AsNoTracking()
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .ProjectTo<CatalogCategoryDto>(mapper.ConfigurationProvider)
            .ToListAsync(ct);

    /// <inheritdoc/>
    public async Task<CatalogCategoryDto> GetAsync(Guid id, CancellationToken ct)
    {
        var category = await db.CatalogCategories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException("找不到分類。");

        return mapper.Map<CatalogCategoryDto>(category);
    }

    /// <inheritdoc/>
    public async Task<CatalogCategoryDto> CreateAsync(CreateCatalogCategoryRequest request, CancellationToken ct)
    {
        var name = request.Name.Trim();

        var slug = request.Slug.Trim().ToLowerInvariant();
        await EnsureSlugUniqueAsync(slug, null, ct);

        if (request.ParentId is { } parentId)
            await EnsureParentExistsAsync(parentId, ct);

        var category = new CatalogCategory
        {
            ParentId = request.ParentId,
            Name = name,
            Slug = slug,
            SortOrder = request.SortOrder,
        };
        db.CatalogCategories.Add(category);

        await db.SaveChangesAsync(ct);

        return mapper.Map<CatalogCategoryDto>(category);
    }

    /// <inheritdoc/>
    public async Task<CatalogCategoryDto> UpdateAsync(Guid id, UpdateCatalogCategoryRequest request, CancellationToken ct)
    {
        var category = await db.CatalogCategories.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException("找不到分類。");

        if (request.Name is not null)
            category.Name = request.Name.Trim();

        if (request.Slug is not null)
        {
            var slug = request.Slug.Trim().ToLowerInvariant();
            await EnsureSlugUniqueAsync(slug, id, ct);
            category.Slug = slug;
        }

        if (request.ParentId is { } parentId)
        {
            if (parentId == id)
                throw new ValidationException("分類的上層不可為自身。");
            await EnsureParentExistsAsync(parentId, ct);
            category.ParentId = parentId;
        }

        if (request.SortOrder is { } sortOrder)
            category.SortOrder = sortOrder;

        await db.SaveChangesAsync(ct);

        return mapper.Map<CatalogCategoryDto>(category);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var category = await db.CatalogCategories.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException("找不到分類。");

        var hasChildren = await db.CatalogCategories.AnyAsync(c => c.ParentId == id, ct);
        if (hasChildren)
            throw new ConflictException("此分類仍有子分類，無法刪除。");

        var referenced = await db.Catalogs.AnyAsync(c => c.CategoryId == id, ct);
        if (referenced)
            throw new ConflictException("此分類仍被商品引用，無法刪除。");

        db.CatalogCategories.Remove(category);
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureSlugUniqueAsync(string slug, Guid? excludeId, CancellationToken ct)
    {
        var used = await db.CatalogCategories.AnyAsync(c => c.Slug == slug && c.Id != excludeId, ct);
        if (used)
            throw new ValidationException("此分類代稱已被使用。");
    }

    private async Task EnsureParentExistsAsync(Guid parentId, CancellationToken ct)
    {
        var exists = await db.CatalogCategories.AnyAsync(c => c.Id == parentId, ct);
        if (!exists)
            throw new ValidationException("指定的上層分類不存在。");
    }
}
