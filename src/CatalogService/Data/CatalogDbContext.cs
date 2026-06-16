using CatalogService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Auth;
using Shared.Data;

namespace CatalogService.Data;

/// <summary>CatalogService 的 EF Core DbContext，繼承 BaseDbContext 取得 Audit 欄位自動填入能力。</summary>
public class CatalogDbContext(DbContextOptions<CatalogDbContext> options, ICurrentUserAccessor currentUser)
    : BaseDbContext(options, currentUser)
{
    /// <summary>商品主檔資料表。</summary>
    public DbSet<Catalog> Catalogs => Set<Catalog>();

    /// <summary>商品展示型資產資料表。</summary>
    public DbSet<CatalogAsset> CatalogAssets => Set<CatalogAsset>();

    /// <summary>商品版本資料表。</summary>
    public DbSet<CatalogVersion> CatalogVersions => Set<CatalogVersion>();

    /// <summary>商品版本可下載檔案資料表。</summary>
    public DbSet<CatalogVersionAsset> CatalogVersionAssets => Set<CatalogVersionAsset>();

    /// <summary>商品分類資料表（平台維護）。</summary>
    public DbSet<CatalogCategory> CatalogCategories => Set<CatalogCategory>();

    /// <summary>商品標籤資料表。</summary>
    public DbSet<CatalogTag> CatalogTags => Set<CatalogTag>();

    /// <summary>商品與標籤多對多關聯資料表。</summary>
    public DbSet<CatalogTagMapping> CatalogTagMappings => Set<CatalogTagMapping>();

    /// <summary>Outbox 訊息資料表。</summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Catalog>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Name).HasMaxLength(200).IsRequired();
            e.Property(c => c.Slug).HasMaxLength(100).IsRequired();
            e.Property(c => c.Currency).HasMaxLength(3).IsRequired();
            e.Property(c => c.Price).HasColumnType("numeric(18,2)");

            // 商品代稱於同一商店內唯一
            e.HasIndex(c => new { c.StoreId, c.Slug }).IsUnique();
            e.HasIndex(c => c.StoreId);
            e.HasIndex(c => c.Status);
            e.HasIndex(c => c.CategoryId);
        });

        model.Entity<CatalogAsset>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.FileName).HasMaxLength(255).IsRequired();
            e.Property(a => a.StorageKey).HasMaxLength(500).IsRequired();
            e.Property(a => a.ContentType).HasMaxLength(100).IsRequired();
            e.HasIndex(a => a.CatalogId);
        });

        model.Entity<CatalogVersion>(e =>
        {
            e.HasKey(v => v.Id);
            e.Property(v => v.Version).HasMaxLength(50).IsRequired();
            e.HasIndex(v => new { v.CatalogId, v.Version }).IsUnique();
        });

        model.Entity<CatalogVersionAsset>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.FileName).HasMaxLength(255).IsRequired();
            e.Property(a => a.StorageKey).HasMaxLength(500).IsRequired();
            e.Property(a => a.ContentType).HasMaxLength(100).IsRequired();
            e.HasIndex(a => a.CatalogVersionId);
        });

        model.Entity<CatalogCategory>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Name).HasMaxLength(100).IsRequired();
            e.Property(c => c.Slug).HasMaxLength(100).IsRequired();
            e.HasIndex(c => c.Slug).IsUnique();
            e.HasIndex(c => c.ParentId);
        });

        model.Entity<CatalogTag>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Name).HasMaxLength(50).IsRequired();
            e.HasIndex(t => t.Name).IsUnique();
        });

        model.Entity<CatalogTagMapping>(e =>
        {
            e.HasKey(m => new { m.CatalogId, m.TagId });
            e.HasIndex(m => m.TagId);
        });

        model.Entity<OutboxMessage>(e =>
        {
            e.HasKey(o => o.Id);
            e.HasIndex(o => o.ProcessedAt);
        });

        base.OnModelCreating(model);
    }
}
