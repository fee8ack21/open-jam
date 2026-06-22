using QuotaService.Models;

namespace QuotaService.Services.Quotas;

/// <summary>資源配額計量、預扣與回滾的業務邏輯。</summary>
public interface IQuotaManager
{
    /// <summary>建立儲存空間預扣（含單檔 / 單商品即時上限檢查）。冪等：同 ReservationId 回傳既有結果。</summary>
    Task<ReservationResponse> ReserveAsync(ReserveRequest request, CancellationToken ct);

    /// <summary>commit 預扣：上傳成功後將預扣轉為實際用量。由 FileReadyEvent 觸發，須冪等。</summary>
    Task CommitAsync(Guid reservationId, long actualSize, CancellationToken ct);

    /// <summary>釋放預扣（取消 / 逾時 / 主動釋放）。HTTP 呼叫時找不到回 404。</summary>
    Task ReleaseAsync(Guid reservationId, CancellationToken ct);

    /// <summary>釋放所有逾時且仍未 commit 的預扣，回傳釋放筆數（sweeper 用）。</summary>
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
