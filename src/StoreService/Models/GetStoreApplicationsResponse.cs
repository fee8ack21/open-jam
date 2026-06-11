namespace StoreService.Models;

/// <summary>開店申請分頁查詢回應。</summary>
public class GetStoreApplicationsResponse
{
    /// <summary>符合條件的總筆數（未分頁）。</summary>
    /// <example>3</example>
    public int TotalCount { get; set; }

    /// <summary>本頁開店申請清單。</summary>
    public List<StoreApplicationDto> Items { get; set; } = [];
}
