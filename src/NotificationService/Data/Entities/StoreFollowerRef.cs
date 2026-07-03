using Shared.Audit;

namespace NotificationService.Data.Entities;

/// <summary>
/// 商店追蹤者參照表（微服務 Ref Table）。來源為 StoreService 的 StoreFollower，
/// 由 StoreFollowerChangedEvent 同步；信箱一律正規化小寫。
/// </summary>
public class StoreFollowerRef : ICreatedAt
{
    /// <summary>參照紀錄唯一識別碼。</summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>商店 ID。</summary>
    public Guid StoreId { get; set; }

    /// <summary>追蹤者電子信箱（小寫）。</summary>
    public string Email { get; set; } = "";

    /// <summary>追蹤者使用者 ID；null 表示訪客憑信箱追蹤，註冊後由 UserRegisteredEvent 回填。</summary>
    public Guid? UserId { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; private set; }
}
