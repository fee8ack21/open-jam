using System.Text.Json;
using ContentService.Data;
using ContentService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bootstrap.Seeders;

/// <summary>
/// Seed 常見問題（FAQ）初始內容至 ContentService 資料庫（取自 portal-web 原前端寫死的 faq）。
/// 主題分類以 slug 冪等 upsert（新增缺漏分類、回填缺漏敘述）；問答項目於 FAQ 表已有任何資料時整體略過，
/// 避免覆蓋後台維護的內容。
/// </summary>
public class FaqSeeder(ContentDbContext db, ILogger<FaqSeeder> logger)
{
    private static readonly string CategoriesPath =
        Path.Combine(AppContext.BaseDirectory, "Resources", "faq", "faq-categories.json");

    private static readonly string ItemsPath =
        Path.Combine(AppContext.BaseDirectory, "Resources", "faq", "faq-items.json");

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>FAQ 分類 seed 資料的一筆項目。</summary>
    private record FaqCategorySeed(string Name, string Slug, string? Description, int SortOrder);

    /// <summary>FAQ 問答 seed 資料的一筆項目（以分類 slug 參照分類）。</summary>
    private record FaqItemSeed(string CategorySlug, string Question, string Answer, int SortOrder);

    public async Task SeedAsync()
    {
        await db.Database.MigrateAsync();

        var categoryIdBySlug = await SeedCategoriesAsync();
        await SeedItemsAsync(categoryIdBySlug);
    }

    /// <summary>以 slug 冪等建立分類，回傳 slug → CategoryId 對照表。</summary>
    private async Task<Dictionary<string, Guid>> SeedCategoriesAsync()
    {
        var json = await File.ReadAllTextAsync(CategoriesPath);
        var seeds = JsonSerializer.Deserialize<List<FaqCategorySeed>>(json, JsonOptions) ?? [];

        var map = new Dictionary<string, Guid>();
        foreach (var seed in seeds)
        {
            var existing = await db.FaqCategories.FirstOrDefaultAsync(c => c.Slug == seed.Slug);
            if (existing is not null)
            {
                // 既有分類僅補回尚未設定的敘述，不覆寫平台後續調整過的其他欄位。
                if (string.IsNullOrWhiteSpace(existing.Description) && !string.IsNullOrWhiteSpace(seed.Description))
                {
                    existing.Description = seed.Description;
                    logger.LogInformation("Backfilled description for FAQ category '{Slug}'", seed.Slug);
                }
                else
                {
                    logger.LogInformation("FAQ category '{Slug}' already exists, skipping", seed.Slug);
                }
                map[seed.Slug] = existing.Id;
                continue;
            }

            var category = new FaqCategory
            {
                Name        = seed.Name,
                Slug        = seed.Slug,
                Description = seed.Description,
                SortOrder   = seed.SortOrder,
            };
            db.FaqCategories.Add(category);
            map[seed.Slug] = category.Id;
            logger.LogInformation("Created FAQ category '{Name}' ({Slug})", seed.Name, seed.Slug);
        }

        await db.SaveChangesAsync();
        return map;
    }

    /// <summary>Seed 問答項目；FAQ 表已有任何資料即整體略過。</summary>
    private async Task SeedItemsAsync(IReadOnlyDictionary<string, Guid> categoryIdBySlug)
    {
        if (await db.FaqItems.AnyAsync())
        {
            logger.LogInformation("FAQ items already exist, skipped");
            return;
        }

        var json = await File.ReadAllTextAsync(ItemsPath);
        var seeds = JsonSerializer.Deserialize<List<FaqItemSeed>>(json, JsonOptions) ?? [];

        var added = 0;
        foreach (var seed in seeds)
        {
            if (!categoryIdBySlug.TryGetValue(seed.CategorySlug, out var categoryId))
            {
                logger.LogWarning("FAQ item skipped: unknown category slug '{Slug}'", seed.CategorySlug);
                continue;
            }

            db.FaqItems.Add(new FaqItem
            {
                CategoryId  = categoryId,
                Question    = seed.Question,
                Answer      = seed.Answer,
                SortOrder   = seed.SortOrder,
                IsPublished = true,
            });
            added++;
        }

        await db.SaveChangesAsync();
        logger.LogInformation("FAQ items seeded: {Count}", added);
    }
}
