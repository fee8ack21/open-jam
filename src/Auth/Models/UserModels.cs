using Auth.Data.Entities;

namespace Auth.Models;

/// <summary>使用者列表查詢請求（分頁採 offset / limit）。</summary>
public class ListUsersRequest
{
    /// <summary>略過筆數。</summary>
    /// <example>0</example>
    public int Offset { get; set; } = 0;

    /// <summary>每頁筆數（最大 100）。</summary>
    /// <example>20</example>
    public int Limit { get; set; } = 20;

    /// <summary>關鍵字（比對電子信箱，包含、不分大小寫）；null 表示不限。</summary>
    /// <example>example.com</example>
    public string? Search { get; set; }

    /// <summary>過濾平台角色；null 表示不限。</summary>
    /// <example>User</example>
    public UserRole? Role { get; set; }

    /// <summary>過濾帳號狀態；null 表示不限。</summary>
    /// <example>Active</example>
    public UserStatus? Status { get; set; }
}

/// <summary>單筆使用者摘要回應（管理員視角，不含密碼雜湊等機敏欄位）。</summary>
public class UserSummaryDto
{
    /// <summary>帳號唯一識別碼。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid Id { get; set; }

    /// <summary>電子信箱。</summary>
    /// <example>creator@example.com</example>
    public string Email { get; set; } = "";

    /// <summary>平台角色。</summary>
    /// <example>User</example>
    public UserRole Role { get; set; }

    /// <summary>帳號狀態。</summary>
    /// <example>Active</example>
    public UserStatus Status { get; set; }

    /// <summary>信箱是否已驗證（帳號狀態已脫離 Pending 即視為已驗證）。</summary>
    /// <example>true</example>
    public bool EmailVerified { get; set; }

    /// <summary>帳號建立時間（UTC）。</summary>
    /// <example>2026-05-02T08:00:00Z</example>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>最後更新時間（UTC）；null 表示自建立後未變更。</summary>
    /// <example>2026-06-10T12:30:00Z</example>
    public DateTimeOffset? UpdatedAt { get; set; }
}

/// <summary>使用者分頁查詢回應。</summary>
public class ListUsersResponse
{
    /// <summary>符合條件的總筆數（未分頁）。</summary>
    /// <example>128</example>
    public int TotalCount { get; set; }

    /// <summary>本頁使用者清單。</summary>
    public List<UserSummaryDto> Items { get; set; } = [];
}
