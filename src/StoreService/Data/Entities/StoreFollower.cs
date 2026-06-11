using Shared.Audit;

namespace StoreService.Data.Entities;

/// <summary>商店追蹤者，憑信箱追蹤，無需註冊帳號。</summary>
public class StoreFollower : ICreatedAt
{
    /// <summary>追蹤紀錄唯一識別碼。</summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>所屬商店 ID。</summary>
    public Guid StoreId { get; set; }

    /// <summary>使用者 ID；null 表示尚未關聯帳號（訪客憑信箱追蹤）。</summary>
    public Guid? UserId { get; set; }

    /// <summary>追蹤者電子信箱。</summary>
    public string Email { get; set; } = "";

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; private set; }
}
