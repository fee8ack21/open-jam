using AutoMapper;
using AutoMapper.QueryableExtensions;
using CatalogService.Data;
using CatalogService.Data.Entities;
using CatalogService.Models;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Services.Tags;

/// <summary>商品標籤業務邏輯實作。</summary>
public class CatalogTagService(CatalogDbContext db, IMapper mapper) : ICatalogTagService
{
    /// <inheritdoc/>
    public async Task<ListCatalogTagsResponse> ListAsync(ListCatalogTagsRequest request, CancellationToken ct)
    {
        var query = db.CatalogTags.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var prefix = request.Search.Trim().ToLowerInvariant();
            query = query.Where(t => EF.Functions.ILike(t.Name, $"{prefix}%"));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(t => t.UsageCount).ThenBy(t => t.Name)
            .Skip(request.Offset)
            .Take(request.Limit)
            .ProjectTo<CatalogTagDto>(mapper.ConfigurationProvider)
            .ToListAsync(ct);

        return new ListCatalogTagsResponse { TotalCount = total, Items = items };
    }

    /// <inheritdoc/>
    public async Task<PopularCatalogTagsResponse> ListPopularAsync(PopularCatalogTagsRequest request, CancellationToken ct)
    {
        var limit = Math.Clamp(request.Limit, 1, 50);

        // 僅計已上架商品的標籤引用（草稿 / 封存 / 停權不列入），確保跑馬燈點擊必有搜尋結果。
        // Catalogs 全域 Query Filter 已排除軟刪除商品。
        var items = await (
            from m in db.CatalogTagMappings
            join c in db.Catalogs on m.CatalogId equals c.Id
            join t in db.CatalogTags on m.TagId equals t.Id
            where c.Status == CatalogStatus.Published
            group t by new { t.Id, t.Name } into g
            orderby g.Count() descending, g.Key.Name
            select new CatalogTagDto { Id = g.Key.Id, Name = g.Key.Name, UsageCount = g.Count() })
            .Take(limit)
            .ToListAsync(ct);

        return new PopularCatalogTagsResponse { Items = items };
    }
}
