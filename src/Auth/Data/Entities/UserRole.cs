namespace Auth.Data.Entities;

/// <summary>平台角色，影響跨服務 API 的授權判斷。</summary>
public enum UserRole
{
    /// <summary>一般使用者。</summary>
    User = 0,

    /// <summary>平台管理員。</summary>
    Admin = 1,
}
