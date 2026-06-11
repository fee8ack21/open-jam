namespace StoreService.Models;

/// <summary>商店追蹤者查詢請求（分頁採 offset / limit）。</summary>
public class GetStoreFollowersRequest
{
    /// <summary>略過筆數。</summary>
    /// <example>0</example>
    public int Offset { get; set; } = 0;

    /// <summary>每頁筆數（最大 100）。</summary>
    /// <example>20</example>
    public int Limit { get; set; } = 20;
}
