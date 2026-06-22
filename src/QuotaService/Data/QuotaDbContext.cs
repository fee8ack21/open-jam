using Microsoft.EntityFrameworkCore;
using QuotaService.Data.Entities;
using Shared.Auth;
using Shared.Data;

namespace QuotaService.Data;

/// <summary>QuotaService 的 EF Core DbContext，繼承 BaseDbContext 取得 Audit 欄位自動填入能力。</summary>
public class QuotaDbContext(DbContextOptions<QuotaDbContext> options, ICurrentUserAccessor currentUser)
    : BaseDbContext(options, currentUser)
{
    /// <summary>租戶用量計數器資料表。</summary>
    public DbSet<TenantUsage> TenantUsages => Set<TenantUsage>();

    /// <summary>儲存空間預扣紀錄資料表。</summary>
    public DbSet<Reservation> Reservations => Set<Reservation>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<TenantUsage>(e =>
        {
            e.HasKey(u => u.TenantId);
            e.Property(u => u.TenantId).ValueGeneratedNever();
        });

        model.Entity<Reservation>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Id).ValueGeneratedNever();
            // sweeper 以 (status, expires_at) 掃描過期預扣；單商品加總以 product_id 過濾。
            e.HasIndex(r => new { r.Status, r.ExpiresAt });
            e.HasIndex(r => r.ProductId);
            e.HasIndex(r => r.TenantId);
        });

        base.OnModelCreating(model);
    }
}
