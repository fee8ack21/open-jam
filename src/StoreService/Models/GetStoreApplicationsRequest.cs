using StoreService.Data.Entities;

namespace StoreService.Models;

/// <summary>開店申請查詢請求（分頁採 offset / limit）。</summary>
public class GetStoreApplicationsRequest
{
    /// <summary>略過筆數。</summary>
    /// <example>0</example>
    public int Offset { get; set; } = 0;

    /// <summary>每頁筆數（最大 100）。</summary>
    /// <example>20</example>
    public int Limit { get; set; } = 20;

    /// <summary>過濾審核狀態。</summary>
    /// <example>Pending</example>
    public StoreApplicationStatus? Status { get; set; }
}
