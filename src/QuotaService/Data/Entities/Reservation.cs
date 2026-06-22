using Shared.Audit;

namespace QuotaService.Data.Entities;

/// <summary>儲存空間預扣紀錄。<see cref="Id"/> 由呼叫方產生並作為冪等鍵，貫穿上傳 → ready → commit 全程。</summary>
public class Reservation : ICreatedAt
{
    /// <summary>預扣紀錄 ID（呼叫方產生的 Guid，冪等鍵 / 跨服務關聯鍵）。</summary>
    public Guid Id { get; set; }

    /// <summary>所屬租戶（創作者）ID。</summary>
    public Guid TenantId { get; set; }

    /// <summary>關聯的商品 ID；用於單商品總量上限加總；null 表示尚未關聯。</summary>
    public Guid? ProductId { get; set; }

    /// <summary>預扣的位元組數。</summary>
    public long Size { get; set; }

    /// <summary>預扣狀態。</summary>
    public ReservationStatus Status { get; set; } = ReservationStatus.Reserved;

    /// <summary>預扣有效期（UTC）；逾時仍未 commit 由 sweeper 釋放。</summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; private set; }
}
