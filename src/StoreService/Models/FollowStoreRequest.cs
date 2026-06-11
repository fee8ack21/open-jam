namespace StoreService.Models;

/// <summary>追蹤／取消追蹤商店請求。</summary>
public class FollowStoreRequest
{
    /// <summary>追蹤者電子信箱。</summary>
    /// <example>follower@example.com</example>
    public string Email { get; set; } = "";
}
