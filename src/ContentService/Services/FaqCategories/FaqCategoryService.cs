using AutoMapper;
using AutoMapper.QueryableExtensions;
using ContentService.Data;
using ContentService.Data.Entities;
using ContentService.Models;
using Microsoft.EntityFrameworkCore;
using Shared.Auth;
using Shared.Exceptions;

namespace ContentService.Services.FaqCategories;

/// <summary>常見問題主題分類管理的具體實作。</summary>
public class FaqCategoryService(
    ContentDbContext db,
    IMapper mapper,
    AuditLogPublisher audit,
    ICurrentUserAccessor currentUser) : IFaqCategoryService
{
    /// <inheritdoc/>
    public async Task<List<FaqCategoryDto>> ListAsync(CancellationToken ct = default) =>
        await db.FaqCategories.AsNoTracking()
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .ProjectTo<FaqCategoryDto>(mapper.ConfigurationProvider)
            .ToListAsync(ct);

    /// <inheritdoc/>
    public async Task<FaqCategoryDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var category = await db.FaqCategories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException("找不到指定的常見問題分類。");

        return mapper.Map<FaqCategoryDto>(category);
    }

    /// <inheritdoc/>
    public async Task<FaqCategoryDto> CreateAsync(CreateFaqCategoryRequest request, CancellationToken ct = default)
    {
        var slug = request.Slug.Trim().ToLowerInvariant();
        await EnsureSlugUniqueAsync(slug, null, ct);

        var category = new FaqCategory
        {
            Name = request.Name.Trim(),
            Slug = slug,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            SortOrder = request.SortOrder,
        };
        db.FaqCategories.Add(category);
        audit.Add(currentUser.UserId, "faq.category.create", "FaqCategory", category.Id);
        await db.SaveChangesAsync(ct);

        return mapper.Map<FaqCategoryDto>(category);
    }

    /// <inheritdoc/>
    public async Task<FaqCategoryDto> UpdateAsync(Guid id, UpdateFaqCategoryRequest request, CancellationToken ct = default)
    {
        var category = await db.FaqCategories.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException("找不到指定的常見問題分類。");

        if (request.Name is not null)
            category.Name = request.Name.Trim();

        if (request.Slug is not null)
        {
            var slug = request.Slug.Trim().ToLowerInvariant();
            await EnsureSlugUniqueAsync(slug, id, ct);
            category.Slug = slug;
        }

        if (request.Description is not null)
            category.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

        if (request.SortOrder is { } sortOrder)
            category.SortOrder = sortOrder;

        audit.Add(currentUser.UserId, "faq.category.update", "FaqCategory", category.Id);
        await db.SaveChangesAsync(ct);

        return mapper.Map<FaqCategoryDto>(category);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var category = await db.FaqCategories.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException("找不到指定的常見問題分類。");

        var referenced = await db.FaqItems.AnyAsync(f => f.CategoryId == id, ct);
        if (referenced)
            throw new ConflictException("此分類仍被常見問題項目引用，無法刪除。");

        db.FaqCategories.Remove(category);
        audit.Add(currentUser.UserId, "faq.category.delete", "FaqCategory", category.Id);
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureSlugUniqueAsync(string slug, Guid? excludeId, CancellationToken ct)
    {
        var used = await db.FaqCategories.AnyAsync(c => c.Slug == slug && c.Id != excludeId, ct);
        if (used)
            throw new ValidationException("此分類代稱已被使用。");
    }
}
