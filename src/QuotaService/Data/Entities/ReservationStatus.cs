namespace QuotaService.Data.Entities;

/// <summary>預扣紀錄狀態。</summary>
public enum ReservationStatus
{
    /// <summary>已預扣，等待上傳完成。</summary>
    Reserved = 0,

    /// <summary>上傳成功並已計入帳號用量。</summary>
    Committed = 1,

    /// <summary>已釋放（取消 / 逾時 / 主動釋放），預扣量已退回。</summary>
    Released = 2,
}
