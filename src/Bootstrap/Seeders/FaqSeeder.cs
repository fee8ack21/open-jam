using System.Text.Json;
using System.Text.Json.Serialization;
using ContentService.Data;
using ContentService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bootstrap.Seeders;

/// <summary>
/// Seed 常見問題（FAQ）初始內容至 ContentService 資料庫（取自 portal-web 原前端寫死的 faq.items）。
/// 冪等：FAQ 表已有任何資料即整體略過，避免覆蓋後台維護的內容。
/// </summary>
public class FaqSeeder(ContentDbContext db, ILogger<FaqSeeder> logger)
{
    private static readonly string ResourcePath =
        Path.Combine(AppContext.BaseDirectory, "Resources", "faq", "faq-items.json");

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>FAQ seed 資料的一筆項目。</summary>
    private record FaqSeedItem(FaqCategory Category, string Question, string Answer, int SortOrder);

    public async Task SeedAsync()
    {
        await db.Database.MigrateAsync();

        if (await db.FaqItems.AnyAsync())
        {
            logger.LogInformation("FAQ items already exist, skipped");
            return;
        }

        var json = await File.ReadAllTextAsync(ResourcePath);
        var items = JsonSerializer.Deserialize<List<FaqSeedItem>>(json, JsonOptions) ?? [];

        foreach (var it in items)
        {
            db.FaqItems.Add(new FaqItem
            {
                Category    = it.Category,
                Question    = it.Question,
                Answer      = it.Answer,
                SortOrder   = it.SortOrder,
                IsPublished = true,
            });
        }

        await db.SaveChangesAsync();
        logger.LogInformation("FAQ items seeded: {Count}", items.Count);
    }
}
