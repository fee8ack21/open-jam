using Auth.Data;
using Auth.Data.Entities;
using Auth.Services.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Bootstrap.Seeders;

public class AdminUserSeeder(AppDbContext db, IPasswordHasher passwordHasher, IConfiguration config, ILogger<AdminUserSeeder> logger)
{
    public async Task SeedAsync()
    {
        await db.Database.MigrateAsync();
        logger.LogInformation("Auth DB migrations applied");

        var email = config["AdminUser:Email"];
        var password = config["AdminUser:Password"];

        var existing = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (existing is not null)
        {
            logger.LogInformation("Admin user '{Email}' already exists, skipping", email);
            return;
        }

        var admin = new User
        {
            Email        = email,
            PasswordHash = passwordHasher.Hash(password),
            Status       = UserStatus.Active,
            Role         = UserRole.Admin,
        };
        db.Users.Add(admin);

        await db.SaveChangesAsync();
        logger.LogInformation("Created admin user '{Email}'", email);
    }
}
