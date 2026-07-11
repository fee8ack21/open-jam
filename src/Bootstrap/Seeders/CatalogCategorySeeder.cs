using CatalogService.Data;
using CatalogService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bootstrap.Seeders;

/// <summary>套用 CatalogService DB migration，並 seed 平台系統預建立的頂層商品分類與其子分類。</summary>
public class CatalogCategorySeeder(CatalogDbContext db, ILogger<CatalogCategorySeeder> logger)
{
    /// <summary>一筆系統預建分類定義（名稱、代稱、補充敘述、同層排序、子分類）。</summary>
    private sealed record CategorySeed(string Name, string Slug, string Description, int SortOrder, CategorySeed[] Children);

    /// <summary>系統預建立的分類樹（頂層分類 + 其下子分類）。頂層敘述沿用前端分類卡片文案。</summary>
    private static readonly CategorySeed[] SystemCategories =
    [
        new("樂譜／音樂", "music", "樂譜、配樂、分軌音檔", 0,
        [
            new("鋼琴譜", "piano-sheet", "鋼琴獨奏與伴奏譜", 0, []),
            new("吉他譜", "guitar-sheet", "木吉他、電吉他譜與 TAB", 1, []),
            new("背景音樂", "background-music", "影片與直播用配樂", 2, []),
            new("音效", "sound-effects", "遊戲與影音音效素材", 3, []),
        ]),
        new("攝影／照片集", "photography", "照片集、RAW、預設", 1,
        [
            new("風景攝影", "landscape", "自然與城市風景作品", 0, []),
            new("人像攝影", "portrait", "人像與寫真作品", 1, []),
            new("庫存素材圖", "stock-photos", "可商用的庫存素材圖", 2, []),
            new("修圖預設集", "photo-presets", "Lightroom／濾鏡預設集", 3, []),
        ]),
        new("電子書／文件", "ebook", "電子書、範本、文件", 2,
        [
            new("小說", "novel", "長篇與短篇小說", 0, []),
            new("漫畫／插畫集", "comic", "漫畫與插畫作品集", 1, []),
            new("教學講義", "study-notes", "課程講義與學習筆記", 2, []),
            new("文件範本", "document-templates", "可編輯的文件範本", 3, []),
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

    /// <summary>以 slug 冪等建立分類；既有則沿用既有 Id 並回填缺漏的敘述。回傳該分類 Id。</summary>
    private async Task<Guid> UpsertAsync(CategorySeed seed, Guid? parentId)
    {
        var existing = await db.CatalogCategories.FirstOrDefaultAsync(c => c.Slug == seed.Slug);
        if (existing is not null)
        {
            // 既有分類僅補回尚未設定的敘述，不覆寫平台後續調整過的其他欄位。
            if (string.IsNullOrWhiteSpace(existing.Description))
            {
                existing.Description = seed.Description;
                logger.LogInformation("Backfilled description for catalog category '{Slug}'", seed.Slug);
            }
            else
            {
                logger.LogInformation("Catalog category '{Slug}' already exists, skipping", seed.Slug);
            }
            // 確保既有系統分類標記為不可刪除（回填舊資料）。
            if (!existing.IsSystem)
            {
                existing.IsSystem = true;
                logger.LogInformation("Marked catalog category '{Slug}' as system category", seed.Slug);
            }
            return existing.Id;
        }

        var category = new CatalogCategory
        {
            ParentId    = parentId,
            Name        = seed.Name,
            Slug        = seed.Slug,
            Description = seed.Description,
            SortOrder   = seed.SortOrder,
            IsSystem    = true,
        };
        db.CatalogCategories.Add(category);
        logger.LogInformation("Created catalog category '{Name}' ({Slug})", seed.Name, seed.Slug);
        return category.Id;
    }
}
