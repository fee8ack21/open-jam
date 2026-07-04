namespace QuotaService.Data.Entities;

/// <summary>扣量紀錄狀態。</summary>
public enum ReservationStatus
{
    /// <summary>已預扣，等待上傳完成（舊制歷史資料，逾時由 sweeper 釋放）。</summary>
    Reserved = 0,

    /// <summary>已計入帳號用量。</summary>
    Committed = 1,

    /// <summary>已釋放（舊制歷史資料），預扣量已退回。</summary>
    Released = 2,
}
