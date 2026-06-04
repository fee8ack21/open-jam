namespace Auth.Data.Entities;

/// <summary>帳號狀態機。</summary>
public enum UserStatus
{
    /// <summary>已註冊但尚未完成信箱驗證，不允許登入。</summary>
    Pending = 0,

    /// <summary>正常使用狀態。</summary>
    Active = 1,

    /// <summary>因連續登入失敗暫時鎖定，逾時後自動回到 Active。</summary>
    Locked = 2,

    /// <summary>遭管理員停權，現有 session 失效並禁止登入。</summary>
    Suspended = 3,

    /// <summary>用戶自行停用，可評估是否允許重新啟用。</summary>
    Deactivated = 4,

    /// <summary>依 GDPR 軟刪除；創作者商品 / 訂單依策略保留或下架。</summary>
    Deleted = 5,
}
