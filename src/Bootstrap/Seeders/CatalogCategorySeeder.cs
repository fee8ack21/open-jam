using CatalogService.Data;
using CatalogService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bootstrap.Seeders;

/// <summary>套用 CatalogService DB migration，並 seed 平台系統預建立的頂層商品分類與其子分類。</summary>
public class CatalogCategorySeeder(CatalogDbContext db, ILogger<CatalogCategorySeeder> logger)
{
    /// <summary>一筆系統預建分類定義（名稱、代稱、同層排序、子分類）。</summary>
    private sealed record CategorySeed(string Name, string Slug, int SortOrder, CategorySeed[] Children);

    /// <summary>系統預建立的分類樹（頂層分類 + 其下子分類）。</summary>
    private static readonly CategorySeed[] SystemCategories =
    [
        new("樂譜／音樂", "music", 0,
        [
            new("鋼琴譜", "piano-sheet", 0, []),
            new("吉他譜", "guitar-sheet", 1, []),
            new("背景音樂", "background-music", 2, []),
            new("音效", "sound-effects", 3, []),
        ]),
        new("攝影／照片集", "photography", 1,
        [
            new("風景攝影", "landscape", 0, []),
            new("人像攝影", "portrait", 1, []),
            new("庫存素材圖", "stock-photos", 2, []),
            new("修圖預設集", "photo-presets", 3, []),
        ]),
        new("電子書／文件", "ebook", 2,
        [
            new("小說", "novel", 0, []),
            new("漫畫／插畫集", "comic", 1, []),
            new("教學講義", "study-notes", 2, []),
            new("文件範本", "document-templates", 3, []),
        ]),
    ];

    public async Task SeedAsync()
    {
        await db.Database.MigrateAsync();
        logger.LogInformation("Catalog DB migrations applied");

        foreach (var top in SystemCategories)
        {
            var parentId = await UpsertAsync(top, null);
            foreach (var child in top.Children)
                await UpsertAsync(child, parentId);
        }

        await db.SaveChangesAsync();
    }

    /// <summary>以 slug 冪等建立分類；回傳該分類 Id（既有則沿用既有 Id）。</summary>
    private async Task<Guid> UpsertAsync(CategorySeed seed, Guid? parentId)
    {
        var existing = await db.CatalogCategories.FirstOrDefaultAsync(c => c.Slug == seed.Slug);
        if (existing is not null)
        {
            logger.LogInformation("Catalog category '{Slug}' already exists, skipping", seed.Slug);
            return existing.Id;
        }

        var category = new CatalogCategory
        {
            ParentId  = parentId,
            Name      = seed.Name,
            Slug      = seed.Slug,
            SortOrder = seed.SortOrder,
        };
        db.CatalogCategories.Add(category);
        logger.LogInformation("Created catalog category '{Name}' ({Slug})", seed.Name, seed.Slug);
        return category.Id;
    }
}
