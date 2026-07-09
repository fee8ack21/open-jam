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
            Path.Combine(ResourcesDir, "terms-of-service.txt"));

        await SeedTypeAsync(
            LegalDocumentType.PrivacyPolicy,
            "隱私權政策",
            Path.Combine(ResourcesDir, "privacy-policy.txt"));

        await db.SaveChangesAsync();
        logger.LogInformation("Legal documents seeded");
    }

    private async Task SeedTypeAsync(LegalDocumentType type, string title, string contentPath)
    {
        if (await db.LegalDocuments.AnyAsync(d => d.Type == type))
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
            Status      = LegalDocumentStatus.Active,
            ActivatedAt = DateTimeOffset.UtcNow,
        });
    }
}
