namespace StoreService.Models;

/// <summary>追蹤／取消追蹤商店請求。</summary>
public class FollowStoreRequest
{
    /// <summary>追蹤者電子信箱。</summary>
    /// <example>follower@example.com</example>
    public string Email { get; set; } = "";
}

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

/// <summary>商店追蹤者分頁查詢回應。</summary>
public class GetStoreFollowersResponse
{
    /// <summary>符合條件的總筆數（未分頁）。</summary>
    /// <example>42</example>
    public int TotalCount { get; set; }

    /// <summary>本頁追蹤者清單。</summary>
    public List<StoreFollowerDto> Items { get; set; } = [];
}

/// <summary>單筆商店追蹤者回應。</summary>
public class StoreFollowerDto
{
    /// <summary>追蹤者電子信箱。</summary>
    /// <example>follower@example.com</example>
    public string Email { get; set; } = "";

    /// <summary>追蹤者使用者 ID；null 表示尚未關聯帳號（訪客憑信箱追蹤）。</summary>
    public Guid? UserId { get; set; }

    /// <summary>追蹤建立時間。</summary>
    public DateTimeOffset CreatedAt { get; set; }
}
