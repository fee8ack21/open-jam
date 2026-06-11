using Shared.Audit;

namespace StoreService.Data.Entities;

/// <summary>開店申請。同一使用者僅能有一筆 Pending 申請；Rejected/Withdrawn 可重新提交。</summary>
public class StoreApplication : ICreatedAt
{
    /// <summary>申請唯一識別碼。</summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>申請人使用者 ID。</summary>
    public Guid UserId { get; set; }

    /// <summary>申請人電子信箱（提交當下自 JWT 取得），供審核結果通知信使用。</summary>
    public string Email { get; set; } = "";

    /// <summary>欲申請的商店顯示名稱。</summary>
    public string StoreName { get; set; } = "";

    /// <summary>欲申請的商店子網域代稱。</summary>
    public string StoreSlug { get; set; } = "";

    /// <summary>審核狀態。</summary>
    public StoreApplicationStatus Status { get; set; } = StoreApplicationStatus.Pending;

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>審核時間；null 表示尚未審核。</summary>
    public DateTimeOffset? ReviewedAt { get; set; }

    /// <summary>審核管理員使用者 ID。</summary>
    public Guid? ReviewedBy { get; set; }

    /// <summary>審核意見，主要用於 Rejected。</summary>
    public string? ReviewComment { get; set; }
}

/// <summary>開店申請審核狀態。</summary>
public enum StoreApplicationStatus
{
    /// <summary>待審核。</summary>
    Pending,

    /// <summary>已核准，已建立 Store + StoreMember(Owner)。</summary>
    Approved,

    /// <summary>已駁回，可重新提交。</summary>
    Rejected,

    /// <summary>申請人自行撤回，可重新提交。</summary>
    Withdrawn,
}
