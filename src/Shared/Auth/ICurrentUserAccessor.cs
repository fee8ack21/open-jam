namespace Shared.Auth;

/// <summary>
/// 提供目前請求的使用者身份，供 BaseDbContext 自動填入 Audit 欄位。
/// 背景工作（Worker / Saga）可提供獨立實作回傳固定的系統帳號 ID。
/// </summary>
public interface ICurrentUserAccessor
{
    /// <summary>目前使用者 ID；未登入或背景工作情境下為 null。</summary>
    Guid? UserId { get; }
}
