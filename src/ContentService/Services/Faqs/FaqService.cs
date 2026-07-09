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

        if (request.Category.HasValue)
            query = query.Where(f => f.Category == request.Category);

        if (request.IsPublished.HasValue)
            query = query.Where(f => f.IsPublished == request.IsPublished);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(f => f.Category)
            .ThenBy(f => f.SortOrder)
            .Skip(request.Offset)
            .Take(request.Limit)
            .ProjectTo<FaqItemDto>(mapper.ConfigurationProvider)
            .ToListAsync(ct);

        return new ListFaqItemsResponse { TotalCount = total, Items = items };
    }

    /// <inheritdoc/>
    public async Task<List<FaqItemDto>> GetPublishedAsync(FaqCategory? category, CancellationToken ct = default)
    {
        var query = db.FaqItems.AsNoTracking().Where(f => f.IsPublished);

        if (category.HasValue)
            query = query.Where(f => f.Category == category);

        return await query
            .OrderBy(f => f.Category)
            .ThenBy(f => f.SortOrder)
            .ProjectTo<FaqItemDto>(mapper.ConfigurationProvider)
            .ToListAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<FaqItemDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var item = await db.FaqItems.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id, ct)
            ?? throw new NotFoundException("找不到指定的常見問題");
        return mapper.Map<FaqItemDto>(item);
    }

    /// <inheritdoc/>
    public async Task<FaqItemDto> CreateAsync(CreateFaqItemRequest request, CancellationToken ct = default)
    {
        var item = new FaqItem
        {
            Category    = request.Category,
            Question    = request.Question.Trim(),
            Answer      = request.Answer,
            SortOrder   = request.SortOrder,
            IsPublished = request.IsPublished,
        };

        db.FaqItems.Add(item);
        audit.Add(currentUser.UserId, "faq.create", "FaqItem", item.Id);
        await db.SaveChangesAsync(ct);

        return mapper.Map<FaqItemDto>(item);
    }

    /// <inheritdoc/>
    public async Task<FaqItemDto> UpdateAsync(Guid id, UpdateFaqItemRequest request, CancellationToken ct = default)
    {
        var item = await db.FaqItems.FirstOrDefaultAsync(f => f.Id == id, ct)
            ?? throw new NotFoundException("找不到指定的常見問題");

        item.Category    = request.Category;
        item.Question    = request.Question.Trim();
        item.Answer      = request.Answer;
        item.SortOrder   = request.SortOrder;
        item.IsPublished = request.IsPublished;
        audit.Add(currentUser.UserId, "faq.update", "FaqItem", item.Id);
        await db.SaveChangesAsync(ct);

        return mapper.Map<FaqItemDto>(item);
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
