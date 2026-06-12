namespace StoreService.Data.Entities;

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
