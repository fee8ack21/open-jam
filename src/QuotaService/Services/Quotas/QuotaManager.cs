using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuotaService.Data;
using QuotaService.Data.Entities;
using QuotaService.Models;
using QuotaService.Options;
using Shared.Auth;
using Shared.Exceptions;

namespace QuotaService.Services.Quotas;

/// <summary>
/// 配額業務邏輯實作。熱路徑（預扣 / commit / release / 商品數）以 DB 原子條件式 SQL 更新計數器，
/// 天然防並發超額；reservation 列本身的 status 作為冪等與順序保護依據。
/// </summary>
public class QuotaManager(
    QuotaDbContext db,
    ICurrentUserAccessor currentUser,
    IOptions<QuotaOptions> options) : IQuotaManager
{
    private readonly QuotaOptions _opts = options.Value;

    /// <inheritdoc/>
    public async Task<ReservationResponse> ReserveAsync(ReserveRequest request, CancellationToken ct)
    {
        var tenantId = currentUser.UserId ?? throw new UnauthorizedException();

        // 單檔上限（需設定，故於 service 層檢查，回 422）
        if (request.Size > _opts.MaxFileSizeBytes)
            throw new ValidationException($"檔案大小超過單檔上限 {_opts.MaxFileSizeBytes} bytes。");

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_opts.ReservationTtlMinutes);

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // 1. lazy upsert 租戶用量（quota 取設定快照）
        await UpsertTenantUsageAsync(tenantId, ct);

        // 2. 冪等插入 reservation：同 ID 重送 → 0 rows，回傳既有結果不重複預扣
        var inserted = await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO reservations (id, tenant_id, product_id, size, status, expires_at, created_at)
            VALUES ({request.ReservationId}, {tenantId}, {request.ProductId}, {request.Size},
                    {(int)ReservationStatus.Reserved}, {expiresAt}, now())
            ON CONFLICT (id) DO NOTHING
            """, ct);

        if (inserted == 0)
        {
            await tx.CommitAsync(ct);
            var existing = await db.Reservations.AsNoTracking()
                .FirstAsync(r => r.Id == request.ReservationId, ct);
            return ToResponse(existing);
        }

        // 3. 單商品總量上限（加總該商品已 committed 用量 + 本次）
        if (request.ProductId is Guid productId)
        {
            var committed = await db.Reservations.AsNoTracking()
                .Where(r => r.ProductId == productId && r.Status == ReservationStatus.Committed)
                .SumAsync(r => (long?)r.Size, ct) ?? 0;

            if (committed + request.Size > _opts.MaxProductTotalBytes)
                throw new ValidationException($"單一商品檔案總量超過上限 {_opts.MaxProductTotalBytes} bytes。");
        }

        // 4. 原子預扣：以影響行數判斷成敗，無需顯式鎖
        var reservedRows = await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE tenant_usages
            SET reserved = reserved + {request.Size}, updated_at = now()
            WHERE tenant_id = {tenantId} AND used + reserved + {request.Size} <= quota
            """, ct);

        if (reservedRows == 0)
            throw new ConflictException("帳號儲存空間配額不足。");

        await tx.CommitAsync(ct);

        return new ReservationResponse
        {
            ReservationId = request.ReservationId,
            Status = ReservationStatus.Reserved,
            ExpiresAt = expiresAt,
        };
    }

    /// <inheritdoc/>
    public async Task CommitAsync(Guid reservationId, long actualSize, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var r = await db.Reservations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == reservationId, ct);
        if (r is null || r.Status == ReservationStatus.Committed)
        {
            await tx.RollbackAsync(ct); // 未知預扣或重複事件：冪等跳過
            return;
        }

        if (r.Status == ReservationStatus.Reserved)
        {
            // used 加實際大小、reserved 釋放當初預扣量（兩者可能不同）
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE tenant_usages
                SET used = used + {actualSize}, reserved = reserved - {r.Size}, updated_at = now()
                WHERE tenant_id = {r.TenantId}
                """, ct);
        }
        else // Released：遲到 commit，reserved 已於 release 扣回，僅補記 used
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE tenant_usages
                SET used = used + {actualSize}, updated_at = now()
                WHERE tenant_id = {r.TenantId}
                """, ct);
        }

        await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE reservations SET status = {(int)ReservationStatus.Committed} WHERE id = {reservationId}
            """, ct);

        await tx.CommitAsync(ct);
    }

    /// <inheritdoc/>
    public async Task ReleaseAsync(Guid reservationId, CancellationToken ct)
    {
        var released = await ReleaseInternalAsync(reservationId, ct);
        if (released is null)
            throw new NotFoundException("找不到預扣紀錄。");
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

    /// <summary>釋放單筆預扣。回傳 null=不存在、false=狀態非 Reserved（no-op）、true=已釋放。</summary>
    private async Task<bool?> ReleaseInternalAsync(Guid reservationId, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

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
    }

    /// <inheritdoc/>
    public async Task ChangeProductCountAsync(int delta, CancellationToken ct)
    {
        var tenantId = currentUser.UserId ?? throw new UnauthorizedException();

        await using var tx = await db.Database.BeginTransactionAsync(ct);
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

    private static ReservationResponse ToResponse(Reservation r) => new()
    {
        ReservationId = r.Id,
        Status = r.Status,
        ExpiresAt = r.ExpiresAt,
    };
}
