using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StoreService.Data;
using StoreService.Data.Entities;
using AuthDbContext = Auth.Data.AppDbContext;

namespace Bootstrap.Seeders;

/// <summary>套用 StoreService DB migration，並為 mock 使用者 seed 對應的店家（以 Owner 成員關聯）。</summary>
public class StoreSeeder(StoreDbContext db, AuthDbContext authDb, IConfiguration config, ILogger<StoreSeeder> logger)
{
    /// <summary>找不到 MockStores 設定時的預設店家 slug。</summary>
    private static readonly string[] DefaultMockSlugs =
    [
        "mateus-asato",
        "matteo-mancuso",
        "seiji-igusa",
        "rick-beato",
    ];

    public async Task SeedAsync()
    {
        await db.Database.MigrateAsync();
        logger.LogInformation("Store DB migrations applied");

        await SeedMockStoresAsync();

        await db.SaveChangesAsync();
    }

    private async Task SeedMockStoresAsync()
    {
        if (!config.GetValue("MockStores:Enabled", false))
        {
            logger.LogInformation("MockStores:Enabled is false, skipping mock store seed");
            return;
        }

        var slugs = config.GetSection("MockStores:Slugs").Get<string[]>();
        if (slugs is null || slugs.Length == 0)
            slugs = DefaultMockSlugs;

        foreach (var slug in slugs)
            await UpsertAsync(slug);
    }

    private async Task UpsertAsync(string slug)
    {
        if (await db.Stores.AnyAsync(s => s.StoreSlug == slug))
        {
            logger.LogInformation("Store '{Slug}' already exists, skipping", slug);
            return;
        }

        // mock 店家的 Owner 即同名 mock 使用者（slug 連字號換成下底線即其 email local part）
        var email = slug.Replace('-', '_') + "@example.com";
        var ownerId = await authDb.Users
            .Where(u => u.Email == email)
            .Select(u => (Guid?)u.Id)
            .FirstOrDefaultAsync();
        if (ownerId is null)
        {
            logger.LogWarning("Owner user '{Email}' for store '{Slug}' not found, skipping", email, slug);
            return;
        }

        var store = new Store
        {
            StoreName = ToDisplayName(slug),
            StoreSlug = slug,
            Status    = StoreStatus.Active,
        };
        db.Stores.Add(store);
        db.StoreMembers.Add(new StoreMember
        {
            StoreId = store.Id,
            UserId  = ownerId.Value,
            Role    = StoreMemberRole.Owner,
        });
        logger.LogInformation("Created store '{Slug}' owned by '{Email}'", slug, email);
    }

    /// <summary>將 slug（如 <c>mateus-asato</c>）轉為顯示名稱（<c>Mateus Asato</c>）。</summary>
    private static string ToDisplayName(string slug) =>
        string.Join(' ', slug.Split('-', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => char.ToUpperInvariant(p[0]) + p[1..]));
}
