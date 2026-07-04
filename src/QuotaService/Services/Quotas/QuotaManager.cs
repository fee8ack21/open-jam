using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuotaService.Data;
using QuotaService.Data.Entities;
using QuotaService.Models;
using QuotaService.Options;
using Shared.Auth;
using Shared.Data;
using Shared.Exceptions;

namespace QuotaService.Services.Quotas;

/// <summary>
/// 配額業務邏輯實作。熱路徑（扣量 / 商品數）以 DB 原子條件式 SQL 更新計數器，
/// 天然防並發超額；charge（reservation）列本身作為冪等與單商品加總依據。
/// </summary>
public class QuotaManager(
    QuotaDbContext db,
    ICurrentUserAccessor currentUser,
    IOptions<QuotaOptions> options) : IQuotaManager
{
    private readonly QuotaOptions _opts = options.Value;

    /// <inheritdoc/>
    public async Task ChargeAsync(ChargeRequest request, CancellationToken ct)
    {
        var tenantId = currentUser.UserId ?? throw new UnauthorizedException();

        // 單檔上限（需設定，故於 service 層檢查，回 422）
        if (request.Size > _opts.MaxFileSizeBytes)
            throw new ValidationException($"檔案大小超過單檔上限 {_opts.MaxFileSizeBytes} bytes。");

        // 啟用 EnableRetryOnFailure 後，顯式交易須包進 execution strategy 才能在 transient 失敗時整體重放。
        await db.Database.ExecuteInTransactionAsync(async tx =>
        {
            // 1. lazy upsert 租戶用量（quota 取設定快照）
            await UpsertTenantUsageAsync(tenantId, ct);

            // 2. 冪等插入扣量紀錄（直接 Committed）：同 ID 重送 → 0 rows，視為已扣過不重複扣量
            var inserted = await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO reservations (id, tenant_id, product_id, size, status, expires_at, created_at)
                VALUES ({request.ChargeId}, {tenantId}, {request.ProductId}, {request.Size},
                        {(int)ReservationStatus.Committed}, now(), now())
                ON CONFLICT (id) DO NOTHING
                """, ct);

            if (inserted == 0)
            {
                await tx.RollbackAsync(ct);
                return;
            }

            // 3. 單商品總量上限（同交易可見本筆，加總即含本次；失敗由 rollback 撤銷本筆）
            if (request.ProductId is Guid productId)
            {
                var committed = await db.Reservations.AsNoTracking()
                    .Where(r => r.ProductId == productId && r.Status == ReservationStatus.Committed)
                    .SumAsync(r => (long?)r.Size, ct) ?? 0;

                if (committed > _opts.MaxProductTotalBytes)
                    throw new ValidationException($"單一商品檔案總量超過上限 {_opts.MaxProductTotalBytes} bytes。");
            }

            // 4. 原子扣量：以影響行數判斷成敗，無需顯式鎖；配額不足由 rollback 撤銷本筆
            var chargedRows = await db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE tenant_usages
                SET used = used + {request.Size}, updated_at = now()
                WHERE tenant_id = {tenantId} AND used + reserved + {request.Size} <= quota
                """, ct);

            if (chargedRows == 0)
                throw new ConflictException("帳號儲存空間配額不足。");

            await tx.CommitAsync(ct);
        }, ct);
    }

    /// <inheritdoc/>
    public async Task<int> ReleaseExpiredAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var expiredIds = await db.Reservations.AsNoTracking()
            .Where(r => r.Status == ReservationStatus.Reserved && r.ExpiresAt < now)
            .Select(r => r.Id)
            .Take(500)
            .ToListAsync(ct);

        var count = 0;
        foreach (var id in expiredIds)
        {
            if (await ReleaseInternalAsync(id, ct) is true)
                count++;
        }

        return count;
    }

    /// <summary>釋放單筆舊制預扣（sweeper 清理歷史資料用）。回傳 null=不存在、false=狀態非 Reserved（no-op）、true=已釋放。</summary>
    private async Task<bool?> ReleaseInternalAsync(Guid reservationId, CancellationToken ct)
    {
        // 啟用 EnableRetryOnFailure 後，顯式交易須包進 execution strategy 才能在 transient 失敗時整體重放。
        return await db.Database.ExecuteInTransactionAsync<bool?>(async tx =>
        {
            var r = await db.Reservations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == reservationId, ct);
            if (r is null)
            {
                await tx.RollbackAsync(ct);
                return null;
            }

            if (r.Status != ReservationStatus.Reserved)
            {
                await tx.RollbackAsync(ct);
                return false;
            }

            await db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE tenant_usages SET reserved = reserved - {r.Size}, updated_at = now()
                WHERE tenant_id = {r.TenantId}
                """, ct);

            await db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE reservations SET status = {(int)ReservationStatus.Released} WHERE id = {reservationId}
                """, ct);

            await tx.CommitAsync(ct);
            return true;
        }, ct);
    }

    /// <inheritdoc/>
    public async Task ChangeProductCountAsync(int delta, CancellationToken ct)
    {
        var tenantId = currentUser.UserId ?? throw new UnauthorizedException();

        // 啟用 EnableRetryOnFailure 後，顯式交易須包進 execution strategy 才能在 transient 失敗時整體重放。
        await db.Database.ExecuteInTransactionAsync(async tx =>
        {
            await UpsertTenantUsageAsync(tenantId, ct);

            if (delta > 0)
            {
                var rows = await db.Database.ExecuteSqlInterpolatedAsync($"""
                    UPDATE tenant_usages SET product_count = product_count + {delta}, updated_at = now()
                    WHERE tenant_id = {tenantId} AND product_count + {delta} <= {_opts.MaxPublishedProducts}
                    """, ct);

                if (rows == 0)
                    throw new ConflictException($"上架商品數已達上限 {_opts.MaxPublishedProducts}。");
            }
            else
            {
                await db.Database.ExecuteSqlInterpolatedAsync($"""
                    UPDATE tenant_usages SET product_count = GREATEST(product_count + {delta}, 0), updated_at = now()
                    WHERE tenant_id = {tenantId}
                    """, ct);
            }

            await tx.CommitAsync(ct);
        }, ct);
    }

    /// <inheritdoc/>
    public async Task<UsageResponse> GetUsageAsync(Guid tenantId, CancellationToken ct)
    {
        var u = await db.TenantUsages.AsNoTracking().FirstOrDefaultAsync(x => x.TenantId == tenantId, ct);

        return new UsageResponse
        {
            TenantId = tenantId,
            Quota = u?.Quota ?? _opts.MaxAccountStorageBytes,
            Used = u?.Used ?? 0,
            Reserved = u?.Reserved ?? 0,
            ProductCount = u?.ProductCount ?? 0,
            MaxProducts = _opts.MaxPublishedProducts,
        };
    }

    /// <inheritdoc/>
    public async Task ReconcileUsedAsync(Guid tenantId, long actualUsedBytes, CancellationToken ct)
    {
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE tenant_usages SET used = {actualUsedBytes}, updated_at = now()
            WHERE tenant_id = {tenantId}
            """, ct);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Guid>> ListTenantIdsAsync(CancellationToken ct) =>
        await db.TenantUsages.AsNoTracking().Select(u => u.TenantId).ToListAsync(ct);

    /// <summary>確保租戶用量列存在；quota 取當下設定快照，已存在則不動。</summary>
    private Task UpsertTenantUsageAsync(Guid tenantId, CancellationToken ct) =>
        db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO tenant_usages (tenant_id, quota, used, reserved, product_count, created_at, updated_at)
            VALUES ({tenantId}, {_opts.MaxAccountStorageBytes}, 0, 0, 0, now(), now())
            ON CONFLICT (tenant_id) DO NOTHING
            """, ct);
}
