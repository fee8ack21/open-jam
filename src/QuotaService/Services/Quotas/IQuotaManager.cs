using QuotaService.Models;

namespace QuotaService.Services.Quotas;

/// <summary>資源配額計量、預扣與回滾的業務邏輯。</summary>
public interface IQuotaManager
{
    /// <summary>
    /// 使用者提交確認後的儲存空間扣量（含單檔 / 單商品 / 帳號總量原子檢查）。
    /// 冪等：同 ChargeId 重送不重複扣量。配額不足拋 409；超單檔 / 單商品上限拋 422。
    /// </summary>
    Task ChargeAsync(ChargeRequest request, CancellationToken ct);

    /// <summary>釋放所有逾時且仍未 commit 的舊制預扣，回傳釋放筆數（sweeper 清理歷史資料用）。</summary>
    Task<int> ReleaseExpiredAsync(CancellationToken ct);

    /// <summary>增減上架商品數（原子檢查上限）。</summary>
    Task ChangeProductCountAsync(int delta, CancellationToken ct);

    /// <summary>查詢指定租戶用量；無紀錄時回傳預設額度與零用量。</summary>
    Task<UsageResponse> GetUsageAsync(Guid tenantId, CancellationToken ct);

    /// <summary>以實際用量校正計數器漂移（每日對帳用）。</summary>
    Task ReconcileUsedAsync(Guid tenantId, long actualUsedBytes, CancellationToken ct);

    /// <summary>取得目前所有租戶 ID（對帳逐一校正用）。</summary>
    Task<IReadOnlyList<Guid>> ListTenantIdsAsync(CancellationToken ct);
}
