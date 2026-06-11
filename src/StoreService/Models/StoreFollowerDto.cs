namespace StoreService.Models;

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
