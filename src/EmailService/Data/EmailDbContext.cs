using EmailService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EmailService.Data;

public class EmailDbContext(DbContextOptions<EmailDbContext> options) : DbContext(options)
{
    public DbSet<EmailRecord>            EmailRecords            => Set<EmailRecord>();
    public DbSet<EmailConfig>            EmailConfigs            => Set<EmailConfig>();
    public DbSet<EmailConfigTranslation> EmailConfigTranslations => Set<EmailConfigTranslation>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<EmailRecord>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasIndex(r => r.OutboxMessageId).IsUnique();
            e.Property(r => r.To).HasMaxLength(255).IsRequired();
            e.Property(r => r.Subject).HasMaxLength(500).IsRequired();
            e.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
        });

        model.Entity<EmailConfig>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).ValueGeneratedOnAdd();
            e.Property(c => c.TemplateKey).HasMaxLength(100).IsRequired();
            e.HasIndex(c => c.TemplateKey).IsUnique();
            e.HasMany(c => c.Translations)
             .WithOne(t => t.EmailConfig)
             .HasForeignKey(t => t.EmailConfigId);

            e.HasData(
                new EmailConfig { Id = 1, TemplateKey = "email.verification",  Description = "帳號開通驗證信", CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new EmailConfig { Id = 2, TemplateKey = "email.password_reset", Description = "密碼重置信",    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            );
        });

        model.Entity<EmailConfigTranslation>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Id).ValueGeneratedOnAdd();
            e.Property(t => t.Locale).HasMaxLength(10).IsRequired();
            e.Property(t => t.Subject).HasMaxLength(500).IsRequired();
            e.HasIndex(t => new { t.EmailConfigId, t.Locale }).IsUnique();

            e.HasData(
                new EmailConfigTranslation
                {
                    Id            = 1,
                    EmailConfigId = 1,
                    Locale        = "zh-TW",
                    Subject       = "Open Jam 帳號驗證",
                    BodyHtml      = """
                        <p>感謝您註冊 Open Jam！</p>
                        <p>請點擊以下連結驗證您的電子信箱：</p>
                        <p><a href="{{VerifyUrl}}" style="display:inline-block;padding:10px 20px;background:#6d28d9;color:#fff;text-decoration:none;border-radius:4px;">驗證帳號</a></p>
                        <p style="color:#6b7280;font-size:14px;">連結將在 {{ExpiresInHours}} 小時後失效。若非您本人操作，請忽略此信。</p>
                        """,
                    CreatedAt     = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new EmailConfigTranslation
                {
                    Id            = 2,
                    EmailConfigId = 2,
                    Locale        = "zh-TW",
                    Subject       = "Open Jam 密碼重置",
                    BodyHtml      = """
                        <p>我們收到了您的密碼重置請求。</p>
                        <p>請點擊以下連結重置您的密碼：</p>
                        <p><a href="{{ResetUrl}}" style="display:inline-block;padding:10px 20px;background:#6d28d9;color:#fff;text-decoration:none;border-radius:4px;">重置密碼</a></p>
                        <p style="color:#6b7280;font-size:14px;">連結將在 {{ExpiresInHours}} 小時後失效。若非您本人操作，請忽略此信。</p>
                        """,
                    CreatedAt     = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        });
    }
}
