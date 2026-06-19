using Auth.Data;
using Auth.Data.Entities;
using Auth.Services.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Bootstrap.Seeders;

/// <summary>套用 Auth DB migration，並 seed 管理員與（可選的）一般使用者 mock 資料。</summary>
public class UserSeeder(AppDbContext db, IPasswordHasher passwordHasher, IConfiguration config, ILogger<UserSeeder> logger)
{
    /// <summary>找不到 MockUsers 設定時的預設一般使用者帳號。</summary>
    private static readonly string[] DefaultMockEmails =
    [
        "mateus_asato@example.com",
        "matteo_mancuso@example.com",
        "seiji_igusa@example.com",
        "rick_beato@example.com",
    ];

    private const string DefaultMockPassword = "Aa123456!";

    public async Task SeedAsync()
    {
        await db.Database.MigrateAsync();
        logger.LogInformation("Auth DB migrations applied");

        await SeedAdminAsync();
        await SeedMockUsersAsync();

        await db.SaveChangesAsync();
    }

    private async Task SeedAdminAsync()
    {
        var email = config["AdminUser:Email"];
        var password = config["AdminUser:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("AdminUser:Email / AdminUser:Password not configured, skipping admin seed");
            return;
        }

        await UpsertAsync(email, password, UserRole.Admin);
    }

    private async Task SeedMockUsersAsync()
    {
        if (!config.GetValue("MockUsers:Enabled", false))
        {
            logger.LogInformation("MockUsers:Enabled is false, skipping mock user seed");
            return;
        }

        var emails = config.GetSection("MockUsers:Emails").Get<string[]>();
        if (emails is null || emails.Length == 0)
            emails = DefaultMockEmails;

        var password = config["MockUsers:Password"];
        if (string.IsNullOrWhiteSpace(password))
            password = DefaultMockPassword;

        foreach (var email in emails)
            await UpsertAsync(email, password, UserRole.User);
    }

    private async Task UpsertAsync(string email, string password, UserRole role)
    {
        if (await db.Users.AnyAsync(u => u.Email == email))
        {
            logger.LogInformation("User '{Email}' already exists, skipping", email);
            return;
        }

        db.Users.Add(new User
        {
            Email        = email,
            PasswordHash = passwordHasher.Hash(password),
            Status       = UserStatus.Active,
            Role         = role,
        });
        logger.LogInformation("Created {Role} user '{Email}'", role, email);
    }
}
