using ContentService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Auth;
using Shared.Data;

namespace ContentService.Data;

/// <summary>
/// ContentService 的 EF Core DbContext，繼承 BaseDbContext 取得 Audit 欄位自動填入能力。
/// 管理平台內容：法律文件（服務條款 / 隱私權政策）與常見問題（FAQ）。
/// </summary>
public class ContentDbContext(DbContextOptions<ContentDbContext> options, ICurrentUserAccessor currentUser)
    : BaseDbContext(options, currentUser)
{
    /// <summary>法律文件資料表。</summary>
    public DbSet<LegalDocument> LegalDocuments => Set<LegalDocument>();

    /// <summary>常見問題主題分類資料表。</summary>
    public DbSet<FaqCategory> FaqCategories => Set<FaqCategory>();

    /// <summary>常見問題資料表。</summary>
    public DbSet<FaqItem> FaqItems => Set<FaqItem>();

    /// <summary>Outbox 訊息資料表。</summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<LegalDocument>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.Title).HasMaxLength(200).IsRequired();
            e.Property(d => d.Content).IsRequired();

            // 同一類型版本序號唯一
            e.HasIndex(d => new { d.Type, d.Version }).IsUnique();

            // 同一類型同時僅一筆 Active（partial unique index）
            e.HasIndex(d => d.Type)
                .IsUnique()
                .HasFilter("status = 1");
        });

        model.Entity<FaqCategory>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Name).HasMaxLength(100).IsRequired();
            e.Property(c => c.Slug).HasMaxLength(100).IsRequired();
            e.Property(c => c.Description).HasMaxLength(200);
            e.HasIndex(c => c.Slug).IsUnique();
            e.HasIndex(c => c.SortOrder);
        });

        model.Entity<FaqItem>(e =>
        {
            e.HasKey(f => f.Id);
            e.Property(f => f.Question).HasMaxLength(500).IsRequired();
            e.Property(f => f.Answer).IsRequired();
            e.HasIndex(f => new { f.CategoryId, f.SortOrder });

            e.HasOne(f => f.Category)
                .WithMany(c => c.Items)
                .HasForeignKey(f => f.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        model.Entity<OutboxMessage>(e =>
        {
            e.HasKey(o => o.Id);
            e.HasIndex(o => o.ProcessedAt);
        });

        base.OnModelCreating(model);
    }
}
