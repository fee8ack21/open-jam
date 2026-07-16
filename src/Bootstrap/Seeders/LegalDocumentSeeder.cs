using ContentService.Data;
using ContentService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bootstrap.Seeders;

/// <summary>
/// Seed 服務條款與隱私權政策的初始啟用版本（v1、Active）至 ContentService 資料庫。
/// 冪等：該類型已有任何版本（不論狀態）即略過，避免覆蓋後台維護的內容。
/// </summary>
public class LegalDocumentSeeder(ContentDbContext db, ILogger<LegalDocumentSeeder> logger)
{
    private static readonly string ResourcesDir =
        Path.Combine(AppContext.BaseDirectory, "Resources", "legal-documents");

    public async Task SeedAsync()
    {
        await db.Database.MigrateAsync();

        await SeedTypeAsync(
            LegalDocumentType.TermsOfService,
            "服務條款",
            Path.Combine(ResourcesDir, "terms-of-service.txt"),
            Path.Combine(ResourcesDir, "terms-of-service.highlights.txt"));

        await SeedTypeAsync(
            LegalDocumentType.PrivacyPolicy,
            "隱私權政策",
            Path.Combine(ResourcesDir, "privacy-policy.txt"),
            Path.Combine(ResourcesDir, "privacy-policy.highlights.txt"));

        await db.SaveChangesAsync();
        logger.LogInformation("Legal documents seeded");
    }

    private async Task SeedTypeAsync(LegalDocumentType type, string title, string contentPath, string highlightsPath)
    {
        // 含已軟刪除的草稿一併檢查：曾有紀錄即代表已初始化過，避免重插 Version = 1 撞唯一索引
        if (await db.LegalDocuments.IgnoreQueryFilters().AnyAsync(d => d.Type == type))
        {
            logger.LogInformation("Legal document {Type} already exists, skipped", type);
            return;
        }

        db.LegalDocuments.Add(new LegalDocument
        {
            Type        = type,
            Version     = 1,
            Title       = title,
            Content     = await File.ReadAllTextAsync(contentPath),
            Highlights  = (await File.ReadAllTextAsync(highlightsPath)).Trim(),
            Status      = LegalDocumentStatus.Active,
            ActivatedAt = DateTimeOffset.UtcNow,
        });
    }
}
