using AutoMapper;
using AutoMapper.QueryableExtensions;
using CatalogService.Data;
using CatalogService.Data.Entities;
using CatalogService.Models;
using Microsoft.EntityFrameworkCore;
using Shared.Auth;
using Shared.Data;
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
    public async Task<CatalogReviewDto> UpsertAsync(Guid catalogId, UpsertReviewRequest request, Guid? orderId, CancellationToken ct)
    {
        var storeId = await db.Catalogs.Where(c => c.Id == catalogId)
            .Select(c => (Guid?)c.StoreId).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("找不到商品。");

        var reviewer = await ResolveReviewerAsync(catalogId, orderId, ct);

        var comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();

        var review = await db.Database.ExecuteInTransactionAsync(async tx =>
        {
            var review = await FindReviewAsync(catalogId, reviewer, ct);

            if (review is null)
            {
                review = new CatalogReview
                {
                    CatalogId = catalogId,
                    ReviewerUserId = reviewer.UserId,
                    ReviewerEmail = reviewer.Email,
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

            auditLog.Add(reviewer.UserId, "catalog.review", "Catalog", catalogId, tenant: storeId);
            await db.SaveChangesAsync(ct);

            await RecomputeAggregateAsync(catalogId, ct);
            await tx.CommitAsync(ct);
            return review;
        }, ct);

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

        // 各星等評論數分佈（跨全部評論的彙總，非僅本頁）。
        var buckets = await db.CatalogReviews.AsNoTracking()
            .Where(r => r.CatalogId == catalogId)
            .GroupBy(r => r.Rating)
            .Select(g => new { Rating = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        var distribution = new int[5];
        foreach (var b in buckets)
        {
            if (b.Rating is >= 1 and <= 5)
                distribution[b.Rating - 1] = b.Count;
        }

        return new ListReviewsResponse
        {
            RatingAverage = agg.RatingAverage,
            RatingCount = agg.RatingCount,
            RatingDistribution = distribution,
            Items = items,
        };
    }

    /// <inheritdoc/>
    public async Task<CatalogReviewDto?> GetMineAsync(Guid catalogId, Guid? orderId, CancellationToken ct)
    {
        // 讀取不需購買驗證：登入者以 JWT 身分、訪客以訂單信箱查詢；訪客訂單無法對應信箱時視為無評論。
        var reviewer = await TryResolveReviewerAsync(catalogId, orderId, ct);
        if (reviewer is null)
            return null;

        return await MatchReviewer(db.CatalogReviews.AsNoTracking(), catalogId, reviewer.Value)
            .ProjectTo<CatalogReviewDto>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(ct);
    }

    /// <inheritdoc/>
    public async Task DeleteMineAsync(Guid catalogId, Guid? orderId, CancellationToken ct)
    {
        var reviewer = await ResolveReviewerAsync(catalogId, orderId, ct);

        var review = await FindReviewAsync(catalogId, reviewer, ct);
        if (review is null)
            return;

        await db.Database.ExecuteInTransactionAsync(async tx =>
        {
            db.CatalogReviews.Remove(review);
            auditLog.Add(reviewer.UserId, "catalog.review.delete", "Catalog", catalogId, tenant: null);
            await db.SaveChangesAsync(ct);

            await RecomputeAggregateAsync(catalogId, ct);
            await tx.CommitAsync(ct);
        }, ct);
    }

    /// <summary>評論者身分：登入買家的 UserId 或訪客的下單信箱（二擇一）。</summary>
    private readonly record struct Reviewer(Guid? UserId, string? Email);

    /// <summary>
    /// 解析評論者身分並驗證其已購買（撰寫 / 刪除用）。登入者轉發 token 向 OrderService 驗證購買；
    /// 未登入者須帶 <paramref name="orderId"/>，驗證訂單已完成且含此商品後取下單信箱。二者皆無則 401、驗證不過則 403。
    /// </summary>
    private async Task<Reviewer> ResolveReviewerAsync(Guid catalogId, Guid? orderId, CancellationToken ct)
    {
        if (currentUser.UserId is Guid userId)
        {
            if (!await orderClient.HasPurchasedAsync(catalogId, ct))
                throw new ForbiddenException("僅購買者可評論此商品。");
            return new Reviewer(userId, null);
        }

        if (orderId is Guid oid)
        {
            var email = await orderClient.ResolveBuyerEmailAsync(oid, catalogId, ct)
                ?? throw new ForbiddenException("僅購買者可評論此商品。");
            return new Reviewer(null, email);
        }

        throw new UnauthorizedException();
    }

    /// <summary>解析評論者身分但不強制存在（讀取用）：無登入身分且訂單無法對應信箱時回 null。</summary>
    private async Task<Reviewer?> TryResolveReviewerAsync(Guid catalogId, Guid? orderId, CancellationToken ct)
    {
        if (currentUser.UserId is Guid userId)
            return new Reviewer(userId, null);

        if (orderId is Guid oid)
        {
            var email = await orderClient.ResolveBuyerEmailAsync(oid, catalogId, ct);
            return email is null ? null : new Reviewer(null, email);
        }

        throw new UnauthorizedException();
    }

    /// <summary>依評論者身分（UserId 或 Email）過濾評論查詢。</summary>
    private static IQueryable<CatalogReview> MatchReviewer(IQueryable<CatalogReview> query, Guid catalogId, Reviewer reviewer) =>
        reviewer.UserId is Guid userId
            ? query.Where(r => r.CatalogId == catalogId && r.ReviewerUserId == userId)
            : query.Where(r => r.CatalogId == catalogId && r.ReviewerEmail == reviewer.Email);

    /// <summary>取得評論者對此商品的既有評論（追蹤，供更新 / 刪除）；無則 null。</summary>
    private Task<CatalogReview?> FindReviewAsync(Guid catalogId, Reviewer reviewer, CancellationToken ct) =>
        MatchReviewer(db.CatalogReviews, catalogId, reviewer).FirstOrDefaultAsync(ct);

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
