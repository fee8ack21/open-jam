using EmailService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Auth;
using Shared.Data;

namespace EmailService.Data;

/// <summary>
/// EmailService 的 EF Core DbContext，繼承 BaseDbContext 取得 Audit 自動填入能力。
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserAccessor currentUser)
    : BaseDbContext(options, currentUser)
{
    /// <summary>信件寄送紀錄資料表。</summary>
    public DbSet<EmailRecord> EmailRecords => Set<EmailRecord>();

    /// <summary>信件模板設定資料表。</summary>
    public DbSet<EmailConfig> EmailConfigs => Set<EmailConfig>();

    /// <summary>信件模板語系翻譯資料表。</summary>
    public DbSet<EmailConfigTranslation> EmailConfigTranslations => Set<EmailConfigTranslation>();

    /// <inheritdoc/>
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
        });

        // 模板種子資料統一由 Bootstrap 的 EmailTemplateSeeder 從 Resources/email-templates 載入，
        // 不在此以 HasData 重複維護。
        model.Entity<EmailConfigTranslation>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Id).ValueGeneratedOnAdd();
            e.Property(t => t.Locale).HasMaxLength(10).IsRequired();
            e.Property(t => t.Subject).HasMaxLength(500).IsRequired();
            e.HasIndex(t => new { t.EmailConfigId, t.Locale }).IsUnique();
        });

        base.OnModelCreating(model);
    }
}
