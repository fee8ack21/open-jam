using Shared.Audit;

namespace QuotaService.Data.Entities;

/// <summary>
/// 儲存空間扣量紀錄。<see cref="Id"/> 由呼叫方產生並作為冪等鍵（慣例 = 檔案 ID）。
/// 新流程於使用者提交確認時直接以 <see cref="ReservationStatus.Committed"/> 入帳；
/// <see cref="ReservationStatus.Reserved"/> / <see cref="ReservationStatus.Released"/> 為舊制預扣歷史資料。
/// </summary>
public class Reservation : ICreatedAt
{
    /// <summary>扣量紀錄 ID（呼叫方產生的 Guid，冪等鍵 / 跨服務關聯鍵）。</summary>
    public Guid Id { get; set; }

    /// <summary>所屬租戶（創作者）ID。</summary>
    public Guid TenantId { get; set; }

    /// <summary>關聯的商品 ID；用於單商品總量上限加總；null 表示尚未關聯。</summary>
    public Guid? ProductId { get; set; }

    /// <summary>扣量的位元組數。</summary>
    public long Size { get; set; }

    /// <summary>紀錄狀態。</summary>
    public ReservationStatus Status { get; set; } = ReservationStatus.Committed;

    /// <summary>舊制預扣有效期（UTC）；逾時仍未 commit 由 sweeper 釋放。新流程直接入帳，此欄位無實質意義。</summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; private set; }
}
