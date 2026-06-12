using StoreService.Data.Entities;

namespace StoreService.Models;

/// <summary>提交開店申請請求。</summary>
public class SubmitStoreApplicationRequest
{
    /// <summary>欲申請的商店顯示名稱。</summary>
    /// <example>小明的數位商店</example>
    public string StoreName { get; set; } = "";

    /// <summary>欲申請的商店子網域代稱。小寫英數字 + 連字號，3–30 字，不可開頭/結尾為連字號。</summary>
    /// <example>xiaoming-shop</example>
    public string StoreSlug { get; set; } = "";
}

/// <summary>駁回開店申請請求。</summary>
public class RejectStoreApplicationRequest
{
    /// <summary>駁回原因，將附於通知信中。</summary>
    /// <example>商店名稱與既有品牌過於相似，請調整後重新申請。</example>
    public string ReviewComment { get; set; } = "";
}

/// <summary>開店申請查詢請求（分頁採 offset / limit）。</summary>
public class GetStoreApplicationsRequest
{
    /// <summary>略過筆數。</summary>
    /// <example>0</example>
    public int Offset { get; set; } = 0;

    /// <summary>每頁筆數（最大 100）。</summary>
    /// <example>20</example>
    public int Limit { get; set; } = 20;

    /// <summary>過濾審核狀態。</summary>
    /// <example>Pending</example>
    public StoreApplicationStatus? Status { get; set; }
}

/// <summary>開店申請分頁查詢回應。</summary>
public class GetStoreApplicationsResponse
{
    /// <summary>符合條件的總筆數（未分頁）。</summary>
    /// <example>3</example>
    public int TotalCount { get; set; }

    /// <summary>本頁開店申請清單。</summary>
    public List<StoreApplicationDto> Items { get; set; } = [];
}

/// <summary>單筆開店申請回應。</summary>
public class StoreApplicationDto
{
    /// <summary>申請唯一識別碼。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid Id { get; set; }

    /// <summary>申請人使用者 ID。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid UserId { get; set; }

    /// <summary>申請人電子信箱。</summary>
    /// <example>creator@example.com</example>
    public string Email { get; set; } = "";

    /// <summary>欲申請的商店顯示名稱。</summary>
    /// <example>小明的數位商店</example>
    public string StoreName { get; set; } = "";

    /// <summary>欲申請的商店子網域代稱。</summary>
    /// <example>xiaoming-shop</example>
    public string StoreSlug { get; set; } = "";

    /// <summary>審核狀態。</summary>
    /// <example>Pending</example>
    public StoreApplicationStatus Status { get; set; }

    /// <summary>提交時間。</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>審核時間；null 表示尚未審核。</summary>
    public DateTimeOffset? ReviewedAt { get; set; }

    /// <summary>審核管理員使用者 ID。</summary>
    public Guid? ReviewedBy { get; set; }

    /// <summary>審核意見，主要用於 Rejected。</summary>
    /// <example>商店名稱與既有品牌過於相似，請調整後重新申請。</example>
    public string? ReviewComment { get; set; }
}
