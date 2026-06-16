using CatalogService.Data;
using CatalogService.Models;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Services.Tags;

/// <summary>商品標籤業務邏輯實作。</summary>
public class CatalogTagService(CatalogDbContext db) : ICatalogTagService
{
    /// <inheritdoc/>
    public async Task<ListCatalogTagsResponse> ListAsync(ListCatalogTagsRequest request, CancellationToken ct)
    {
        var limit = Math.Clamp(request.Limit, 1, 100);
        var offset = Math.Max(request.Offset, 0);

        var query = db.CatalogTags.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var prefix = request.Search.Trim().ToLowerInvariant();
            query = query.Where(t => EF.Functions.ILike(t.Name, $"{prefix}%"));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(t => t.UsageCount).ThenBy(t => t.Name)
            .Skip(offset)
            .Take(limit)
            .Select(t => new CatalogTagDto
            {
                Id = t.Id,
                Name = t.Name,
                UsageCount = t.UsageCount,
            })
            .ToListAsync(ct);

        return new ListCatalogTagsResponse { TotalCount = total, Items = items };
    }
}
