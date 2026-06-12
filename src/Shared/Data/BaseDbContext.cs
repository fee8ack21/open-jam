using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Shared.Audit;
using Shared.Auth;

namespace Shared.Data;

/// <summary>
/// 所有 DbContext 的共用基底，於 SaveChangesAsync 時自動填入 Audit 欄位。
/// 軟刪除（DeletedAt / DeletedBy）由業務層呼叫 Remove() 觸發：此處偵測到實作 IDeletedAt
/// 的 Entity 被刪除時，自動轉為軟刪除並填入 DeletedAt / DeletedBy，避免資料被真正刪除。
/// Audit 欄位於各 Entity 一律宣告為 private set，僅能由此自動賦值，不可在外部手動指派。
/// </summary>
public abstract class BaseDbContext(DbContextOptions options, ICurrentUserAccessor currentUser)
    : DbContext(options)
{
    /// <summary>儲存變更並自動填入 Audit 欄位；將實作 IDeletedAt 的 Entity 的刪除動作轉為軟刪除。</summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now    = DateTimeOffset.UtcNow;
        var userId = currentUser.UserId;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Deleted && entry.Entity is IDeletedAt)
            {
                entry.State = EntityState.Modified;
                entry.Property(nameof(IDeletedAt.DeletedAt)).CurrentValue = now;

                if (entry.Entity is IDeletedBy)
                    entry.Property(nameof(IDeletedBy.DeletedBy)).CurrentValue = userId;
            }

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

    /// <summary>為實作 IDeletedAt 的 Entity 套用全域 Query Filter，自動排除已軟刪除的資料。子類別覆寫 OnModelCreating 時須呼叫 base.OnModelCreating(modelBuilder)。</summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(IDeletedAt).IsAssignableFrom(entityType.ClrType)) continue;

            typeof(BaseDbContext)
                .GetMethod(nameof(ApplySoftDeleteFilter), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(entityType.ClrType)
                .Invoke(null, [modelBuilder]);
        }
    }

    private static void ApplySoftDeleteFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, IDeletedAt
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => e.DeletedAt == null);
    }
}
