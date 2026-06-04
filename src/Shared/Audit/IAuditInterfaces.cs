namespace Shared.Audit;

/// <summary>實作此介面的 Entity 在新增時由 BaseDbContext 自動填入建立時間。</summary>
public interface ICreatedAt
{
    /// <summary>建立時間（UTC）。</summary>
    DateTimeOffset CreatedAt { get; }
}

/// <summary>實作此介面的 Entity 在新增時由 BaseDbContext 自動填入建立者 ID。</summary>
public interface ICreatedBy
{
    /// <summary>建立者使用者 ID。</summary>
    Guid CreatedBy { get; }
}

/// <summary>實作此介面的 Entity 在新增或修改時由 BaseDbContext 自動填入更新時間。</summary>
public interface IUpdatedAt
{
    /// <summary>最後更新時間（UTC）。</summary>
    DateTimeOffset? UpdatedAt { get; }
}

/// <summary>實作此介面的 Entity 在新增或修改時由 BaseDbContext 自動填入更新者 ID。</summary>
public interface IUpdatedBy
{
    /// <summary>最後更新者使用者 ID。</summary>
    Guid? UpdatedBy { get; }
}

/// <summary>實作此介面的 Entity 支援軟刪除，由業務層呼叫 SoftDelete 方法填入刪除時間。</summary>
public interface IDeletedAt
{
    /// <summary>軟刪除時間（UTC）；null 表示未刪除。</summary>
    DateTimeOffset? DeletedAt { get; }
}

/// <summary>實作此介面的 Entity 支援軟刪除，由業務層呼叫 SoftDelete 方法填入刪除者 ID。</summary>
public interface IDeletedBy
{
    /// <summary>軟刪除操作者使用者 ID。</summary>
    Guid? DeletedBy { get; }
}
