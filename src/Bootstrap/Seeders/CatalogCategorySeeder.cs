using CatalogService.Data;
using CatalogService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bootstrap.Seeders;

/// <summary>套用 CatalogService DB migration，並 seed 平台系統預建立的頂層商品分類。</summary>
public class CatalogCategorySeeder(CatalogDbContext db, ILogger<CatalogCategorySeeder> logger)
{
    /// <summary>系統預建立的頂層分類（名稱、代稱、同層排序）。</summary>
    private static readonly (string Name, string Slug, int SortOrder)[] SystemCategories =
    [
        ("樂譜／音樂", "music", 0),
        ("攝影／照片集", "photography", 1),
        ("電子書／文件", "ebook", 2),
    ];

    public async Task SeedAsync()
    {
        await db.Database.MigrateAsync();
        logger.LogInformation("Catalog DB migrations applied");

        foreach (var (name, slug, sortOrder) in SystemCategories)
        {
            if (await db.CatalogCategories.AnyAsync(c => c.Slug == slug))
            {
                logger.LogInformation("Catalog category '{Slug}' already exists, skipping", slug);
                continue;
            }

            db.CatalogCategories.Add(new CatalogCategory
            {
                ParentId  = null,
                Name      = name,
                Slug      = slug,
                SortOrder = sortOrder,
            });
            logger.LogInformation("Created catalog category '{Name}' ({Slug})", name, slug);
        }

        await db.SaveChangesAsync();
    }
}
