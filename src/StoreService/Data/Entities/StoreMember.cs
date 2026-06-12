namespace StoreService.Data.Entities;

/// <summary>商店成員。MVP 僅支援 Owner，由開店申請核准時自動建立。</summary>
public class StoreMember
{
    /// <summary>成員紀錄唯一識別碼。</summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>所屬商店 ID。</summary>
    public Guid StoreId { get; set; }

    /// <summary>使用者 ID。</summary>
    public Guid UserId { get; set; }

    /// <summary>成員角色。</summary>
    public StoreMemberRole Role { get; set; }
}
