using Shared.Audit;

namespace QuotaService.Data.Entities;

/// <summary>單一租戶（創作者）的資源用量計數器。熱路徑以原子條件式 SQL 更新，避免顯式鎖。</summary>
public class TenantUsage : ICreatedAt
{
    /// <summary>租戶 ID（創作者使用者 ID，取自 JWT sub）。</summary>
    public Guid TenantId { get; set; }

    /// <summary>儲存空間配額上限（bytes）；建列當下從設定快照。</summary>
    public long Quota { get; set; }

    /// <summary>已實際使用的儲存空間（bytes）。</summary>
    public long Used { get; set; }

    /// <summary>已預扣但尚未 commit 的儲存空間（bytes）。</summary>
    public long Reserved { get; set; }

    /// <summary>目前上架（Published）商品數。</summary>
    public int ProductCount { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>最後更新時間（UTC）；由原子更新 SQL 直接寫入 now()。</summary>
    public DateTimeOffset UpdatedAt { get; set; }
}
