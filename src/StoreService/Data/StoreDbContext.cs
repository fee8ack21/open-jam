using Microsoft.EntityFrameworkCore;
using Shared.Auth;
using Shared.Data;
using StoreService.Data.Entities;

namespace StoreService.Data;

/// <summary>
/// StoreService 的 EF Core DbContext，繼承 BaseDbContext 取得 Audit 欄位自動填入能力。
/// </summary>
public class StoreDbContext(DbContextOptions<StoreDbContext> options, ICurrentUserAccessor currentUser)
    : BaseDbContext(options, currentUser)
{
    /// <summary>商店資料表。</summary>
    public DbSet<Store> Stores => Set<Store>();

    /// <summary>商店成員資料表。</summary>
    public DbSet<StoreMember> StoreMembers => Set<StoreMember>();

    /// <summary>開店申請資料表。</summary>
    public DbSet<StoreApplication> StoreApplications => Set<StoreApplication>();

    /// <summary>商店追蹤者資料表。</summary>
    public DbSet<StoreFollower> StoreFollowers => Set<StoreFollower>();

    /// <summary>已上傳檔案的落地紀錄資料表。</summary>
    public DbSet<Asset> Assets => Set<Asset>();

    /// <summary>Outbox 訊息資料表。</summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Store>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => s.StoreSlug).IsUnique();
            e.Property(s => s.StoreName).HasMaxLength(100).IsRequired();
            e.Property(s => s.StoreSlug).HasMaxLength(30).IsRequired();
        });

        model.Entity<StoreMember>(e =>
        {
            e.HasKey(m => m.Id);
            e.HasIndex(m => new { m.StoreId, m.UserId }).IsUnique();
        });

        model.Entity<StoreApplication>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Email).HasMaxLength(255).IsRequired();
            e.Property(a => a.StoreName).HasMaxLength(100).IsRequired();
            e.Property(a => a.StoreSlug).HasMaxLength(30).IsRequired();

            // 同一使用者僅能有一筆 Pending 申請
            e.HasIndex(a => a.UserId)
                .IsUnique()
                .HasFilter("status = 0");
        });

        model.Entity<StoreFollower>(e =>
        {
            e.HasKey(f => f.Id);
            e.HasIndex(f => new { f.StoreId, f.Email }).IsUnique();
            e.Property(f => f.Email).HasMaxLength(255).IsRequired();
        });

        model.Entity<Asset>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.StorageKey).HasMaxLength(500).IsRequired();
            e.Property(a => a.FileName).HasMaxLength(255).IsRequired();
            e.Property(a => a.ContentType).HasMaxLength(100).IsRequired();
        });

        model.Entity<OutboxMessage>(e =>
        {
            e.HasKey(o => o.Id);
            e.HasIndex(o => o.ProcessedAt);
        });

        base.OnModelCreating(model);
    }
}
