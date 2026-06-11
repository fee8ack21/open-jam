namespace StoreService.Models;

/// <summary>商店追蹤者分頁查詢回應。</summary>
public class GetStoreFollowersResponse
{
    /// <summary>符合條件的總筆數（未分頁）。</summary>
    /// <example>42</example>
    public int TotalCount { get; set; }

    /// <summary>本頁追蹤者清單。</summary>
    public List<StoreFollowerDto> Items { get; set; } = [];
}
