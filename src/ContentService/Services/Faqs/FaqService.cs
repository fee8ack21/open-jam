using AutoMapper;
using AutoMapper.QueryableExtensions;
using ContentService.Data;
using ContentService.Data.Entities;
using ContentService.Models;
using ContentService.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Auth;
using Shared.Exceptions;

namespace ContentService.Services.Faqs;

/// <summary>常見問題管理的具體實作。</summary>
public class FaqService(
    ContentDbContext db,
    IMapper mapper,
    AuditLogPublisher audit,
    ICurrentUserAccessor currentUser) : IFaqService
{
    /// <inheritdoc/>
    public async Task<ListFaqItemsResponse> ListAsync(ListFaqItemsRequest request, CancellationToken ct = default)
    {
        var query = db.FaqItems.AsNoTracking();

        if (request.CategoryId.HasValue)
            query = query.Where(f => f.CategoryId == request.CategoryId);

        if (request.IsPublished.HasValue)
            query = query.Where(f => f.IsPublished == request.IsPublished);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(f => f.Category.SortOrder)
            .ThenBy(f => f.SortOrder)
            .Skip(request.Offset)
            .Take(request.Limit)
            .ProjectTo<FaqItemDto>(mapper.ConfigurationProvider)
            .ToListAsync(ct);

        return new ListFaqItemsResponse { TotalCount = total, Items = items };
    }

    /// <inheritdoc/>
    public async Task<List<FaqItemDto>> GetPublishedAsync(Guid? categoryId, CancellationToken ct = default)
    {
        var query = db.FaqItems.AsNoTracking().Where(f => f.IsPublished);

        if (categoryId.HasValue)
            query = query.Where(f => f.CategoryId == categoryId);

        return await query
            .OrderBy(f => f.Category.SortOrder)
            .ThenBy(f => f.SortOrder)
            .ProjectTo<FaqItemDto>(mapper.ConfigurationProvider)
            .ToListAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<FaqItemDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var item = await db.FaqItems.AsNoTracking()
            .Where(f => f.Id == id)
            .ProjectTo<FaqItemDto>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("找不到指定的常見問題");
        return item;
    }

    /// <inheritdoc/>
    public async Task<FaqItemDto> CreateAsync(CreateFaqItemRequest request, CancellationToken ct = default)
    {
        await EnsureCategoryExistsAsync(request.CategoryId, ct);

        var item = new FaqItem
        {
            CategoryId  = request.CategoryId,
            Question    = request.Question.Trim(),
            Answer      = request.Answer,
            SortOrder   = request.SortOrder,
            IsPublished = request.IsPublished,
        };

        db.FaqItems.Add(item);
        audit.Add(currentUser.UserId, "faq.create", "FaqItem", item.Id);
        await db.SaveChangesAsync(ct);

        return await GetAsync(item.Id, ct);
    }

    /// <inheritdoc/>
    public async Task<FaqItemDto> UpdateAsync(Guid id, UpdateFaqItemRequest request, CancellationToken ct = default)
    {
        var item = await db.FaqItems.FirstOrDefaultAsync(f => f.Id == id, ct)
            ?? throw new NotFoundException("找不到指定的常見問題");

        await EnsureCategoryExistsAsync(request.CategoryId, ct);

        item.CategoryId  = request.CategoryId;
        item.Question    = request.Question.Trim();
        item.Answer      = request.Answer;
        item.SortOrder   = request.SortOrder;
        item.IsPublished = request.IsPublished;
        audit.Add(currentUser.UserId, "faq.update", "FaqItem", item.Id);
        await db.SaveChangesAsync(ct);

        return await GetAsync(item.Id, ct);
    }

    /// <summary>確認指定分類存在，否則拋 <see cref="ValidationException"/>。</summary>
    private async Task EnsureCategoryExistsAsync(Guid categoryId, CancellationToken ct)
    {
        var exists = await db.FaqCategories.AnyAsync(c => c.Id == categoryId, ct);
        if (!exists)
            throw new ValidationException("指定的常見問題分類不存在。");
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var item = await db.FaqItems.FirstOrDefaultAsync(f => f.Id == id, ct)
            ?? throw new NotFoundException("找不到指定的常見問題");

        db.FaqItems.Remove(item);
        audit.Add(currentUser.UserId, "faq.delete", "FaqItem", item.Id);
        await db.SaveChangesAsync(ct);
    }
}
