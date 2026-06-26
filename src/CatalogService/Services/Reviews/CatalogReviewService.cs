using AutoMapper;
using AutoMapper.QueryableExtensions;
using CatalogService.Data;
using CatalogService.Data.Entities;
using CatalogService.Models;
using Microsoft.EntityFrameworkCore;
using Shared.Auth;
using Shared.Exceptions;

namespace CatalogService.Services.Reviews;

/// <summary>商品評論業務邏輯實作。撰寫評論前以 OrderService 驗證購買；彙總（平均分 / 評論數）回寫 Catalog。</summary>
public class CatalogReviewService(
    CatalogDbContext db,
    ICurrentUserAccessor currentUser,
    OrderServiceClient orderClient,
    AuditLogPublisher auditLog,
    IMapper mapper) : ICatalogReviewService
{
    /// <inheritdoc/>
    public async Task<CatalogReviewDto> UpsertAsync(Guid catalogId, UpsertReviewRequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException();

        var storeId = await db.Catalogs.Where(c => c.Id == catalogId)
            .Select(c => (Guid?)c.StoreId).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("找不到商品。");

        // 購買驗證：僅已完成訂單購買此商品者可評論。
        if (!await orderClient.HasPurchasedAsync(catalogId, ct))
            throw new ForbiddenException("僅購買者可評論此商品。");

        var comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var review = await db.CatalogReviews
            .FirstOrDefaultAsync(r => r.CatalogId == catalogId && r.ReviewerUserId == userId, ct);

        if (review is null)
        {
            review = new CatalogReview
            {
                CatalogId = catalogId,
                ReviewerUserId = userId,
                Rating = request.Rating,
                Comment = comment,
            };
            db.CatalogReviews.Add(review);
        }
        else
        {
            review.Rating = request.Rating;
            review.Comment = comment;
        }

        auditLog.Add(userId, "catalog.review", "Catalog", catalogId, tenant: storeId);
        await db.SaveChangesAsync(ct);

        await RecomputeAggregateAsync(catalogId, ct);
        await tx.CommitAsync(ct);

        return mapper.Map<CatalogReviewDto>(review);
    }

    /// <inheritdoc/>
    public async Task<ListReviewsResponse> ListAsync(Guid catalogId, ListReviewsRequest request, CancellationToken ct)
    {
        var agg = await db.Catalogs.AsNoTracking()
            .Where(c => c.Id == catalogId)
            .Select(c => new { c.RatingAverage, c.RatingCount })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("找不到商品。");

        var items = await db.CatalogReviews.AsNoTracking()
            .Where(r => r.CatalogId == catalogId)
            .OrderByDescending(r => r.CreatedAt)
            .Skip(request.Offset)
            .Take(request.Limit)
            .ProjectTo<CatalogReviewDto>(mapper.ConfigurationProvider)
            .ToListAsync(ct);

        return new ListReviewsResponse
        {
            RatingAverage = agg.RatingAverage,
            RatingCount = agg.RatingCount,
            Items = items,
        };
    }

    /// <inheritdoc/>
    public async Task<CatalogReviewDto?> GetMineAsync(Guid catalogId, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException();

        return await db.CatalogReviews.AsNoTracking()
            .Where(r => r.CatalogId == catalogId && r.ReviewerUserId == userId)
            .ProjectTo<CatalogReviewDto>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(ct);
    }

    /// <inheritdoc/>
    public async Task DeleteMineAsync(Guid catalogId, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException();

        var review = await db.CatalogReviews
            .FirstOrDefaultAsync(r => r.CatalogId == catalogId && r.ReviewerUserId == userId, ct);
        if (review is null)
            return;

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        db.CatalogReviews.Remove(review);
        auditLog.Add(userId, "catalog.review.delete", "Catalog", catalogId, tenant: null);
        await db.SaveChangesAsync(ct);

        await RecomputeAggregateAsync(catalogId, ct);
        await tx.CommitAsync(ct);
    }

    /// <summary>由評論重算商品的平均分 / 評論數並回寫 Catalog（不更動 Audit 欄位）。</summary>
    private async Task RecomputeAggregateAsync(Guid catalogId, CancellationToken ct)
    {
        var stats = await db.CatalogReviews.AsNoTracking()
            .Where(r => r.CatalogId == catalogId)
            .GroupBy(_ => 1)
            .Select(g => new { Count = g.Count(), Sum = g.Sum(r => r.Rating) })
            .FirstOrDefaultAsync(ct);

        var count = stats?.Count ?? 0;
        var average = count == 0 ? 0 : Math.Round((double)stats!.Sum / count, 2);

        await db.Catalogs
            .Where(c => c.Id == catalogId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.RatingCount, count)
                .SetProperty(c => c.RatingAverage, average), ct);
    }
}
