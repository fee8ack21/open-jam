using Microsoft.EntityFrameworkCore;
using Shared.Auth;
using Shared.Data;
using StorageService.Data.Entities;

namespace StorageService.Data;

/// <summary>StorageService 的 EF Core DbContext。</summary>
public class StorageDbContext(DbContextOptions<StorageDbContext> options, ICurrentUserAccessor currentUser)
    : BaseDbContext(options, currentUser)
{
    /// <summary>數位商品檔案紀錄資料表。</summary>
    public DbSet<StoredFile> StoredFiles => Set<StoredFile>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<StoredFile>(e =>
        {
            e.HasKey(f => f.Id);
            e.HasIndex(f => f.CreatorId);
            e.HasIndex(f => f.ProductId);
            e.HasIndex(f => new { f.Status, f.CreatedAt });
            e.HasIndex(f => f.ReferencedAt);
            e.Property(f => f.StorageKey).HasMaxLength(1024).IsRequired();
            e.Property(f => f.OriginalName).HasMaxLength(512).IsRequired();
            e.Property(f => f.ContentType).HasMaxLength(128).IsRequired();
            e.Property(f => f.FileType).HasConversion<string>().HasMaxLength(20);
            e.Property(f => f.Status).HasConversion<string>().HasMaxLength(20);
        });

        base.OnModelCreating(model);
    }
}
