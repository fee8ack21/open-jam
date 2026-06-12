using LogService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Auth;
using Shared.Data;

namespace LogService.Data;

/// <summary>
/// LogService 的 EF Core DbContext，繼承 BaseDbContext 取得 CreatedAt 自動填入能力。
/// AuditLog 為 Append-only：不在此 DbContext 提供 SaveChanges 以外的更新路徑。
/// </summary>
public class LogDbContext(DbContextOptions<LogDbContext> options, ICurrentUserAccessor currentUser)
    : BaseDbContext(options, currentUser)
{
    /// <summary>稽核事件紀錄資料表。</summary>
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<AuditLog>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasIndex(a => a.OutboxMessageId).IsUnique();
            e.HasIndex(a => a.OccurredAt);
            e.HasIndex(a => a.Tenant);
            e.HasIndex(a => a.Who);
            e.Property(a => a.Action).HasMaxLength(100).IsRequired();
            e.Property(a => a.Target).HasMaxLength(100).IsRequired();
            e.Property(a => a.Result).HasMaxLength(20).IsRequired();
            e.Property(a => a.Ip).HasMaxLength(45);
            e.Property(a => a.CorrelationId).HasMaxLength(255);
        });

        base.OnModelCreating(model);
    }
}
