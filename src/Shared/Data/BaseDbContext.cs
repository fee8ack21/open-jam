using Microsoft.EntityFrameworkCore;
using Shared.Audit;
using Shared.Auth;

namespace Shared.Data;

/// <summary>
/// 所有 DbContext 的共用基底，於 SaveChangesAsync 時自動填入 Audit 欄位。
/// 軟刪除（DeletedAt / DeletedBy）由業務層顯式觸發，此處不自動處理。
/// </summary>
public abstract class BaseDbContext(DbContextOptions options, ICurrentUserAccessor currentUser)
    : DbContext(options)
{
    /// <summary>儲存變更並自動填入 Audit 欄位。</summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now    = DateTimeOffset.UtcNow;
        var userId = currentUser.UserId;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity is ICreatedAt)
                    entry.Property(nameof(ICreatedAt.CreatedAt)).CurrentValue = now;

                if (entry.Entity is ICreatedBy && userId.HasValue)
                    entry.Property(nameof(ICreatedBy.CreatedBy)).CurrentValue = userId.Value;
            }

            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                if (entry.Entity is IUpdatedAt)
                    entry.Property(nameof(IUpdatedAt.UpdatedAt)).CurrentValue = now;

                if (entry.Entity is IUpdatedBy && userId.HasValue)
                    entry.Property(nameof(IUpdatedBy.UpdatedBy)).CurrentValue = userId.Value;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
