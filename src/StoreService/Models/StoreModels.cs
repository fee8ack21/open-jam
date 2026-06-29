using StoreService.Data.Entities;

namespace StoreService.Models;

/// <summary>全平台商店列表查詢請求（分頁採 offset / limit）。僅 Admin 使用。</summary>
public class ListStoresRequest
{
    /// <summary>限定商店狀態；null 表示不限。</summary>
    /// <example>Active</example>
    public StoreStatus? Status { get; set; }

    /// <summary>名稱 / 代稱關鍵字搜尋；null 表示不限。</summary>
    /// <example>小明</example>
    public string? Search { get; set; }

    /// <summary>略過筆數。</summary>
    /// <example>0</example>
    public int Offset { get; set; } = 0;

    /// <summary>每頁筆數（最大 100）。</summary>
    /// <example>20</example>
    public int Limit { get; set; } = 20;
}

/// <summary>全平台商店列表分頁回應。</summary>
public class ListStoresResponse
{
    /// <summary>符合條件的總筆數（未分頁）。</summary>
    /// <example>42</example>
    public int TotalCount { get; set; }

    /// <summary>本頁商店清單。</summary>
    public List<StoreDto> Items { get; set; } = [];
}

/// <summary>商店基本資訊回應。</summary>
public class StoreDto
{
    /// <summary>商店唯一識別碼。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid Id { get; set; }

    /// <summary>商店顯示名稱。</summary>
    /// <example>小明的數位商店</example>
    public string StoreName { get; set; } = "";

    /// <summary>商店子網域代稱。</summary>
    /// <example>xiaoming-shop</example>
    public string StoreSlug { get; set; } = "";

    /// <summary>商店描述。</summary>
    /// <example>專注於數位插畫與素材販售。</example>
    public string? Description { get; set; }

    /// <summary>商店頭像公開 URL；null 表示尚未設定。</summary>
    /// <example>http://localhost:5171/v1/files/blob/public/3fa85f64-5717-4562-b3fc-2c963f66afa6/avatar.png</example>
    public string? AvatarUrl { get; set; }

    /// <summary>商店橫幅公開 URL；null 表示尚未設定。</summary>
    /// <example>http://localhost:5171/v1/files/blob/public/3fa85f64-5717-4562-b3fc-2c963f66afa6/banner.png</example>
    public string? BannerUrl { get; set; }

    /// <summary>商店狀態。</summary>
    /// <example>Active</example>
    public StoreStatus Status { get; set; }

    /// <summary>建立時間。</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>最後更新時間。</summary>
    public DateTimeOffset? UpdatedAt { get; set; }
}

/// <summary>登入使用者所屬商店資訊。</summary>
public class MyStoreDto
{
    /// <summary>商店基本資訊。</summary>
    public StoreDto Store { get; set; } = new();

    /// <summary>使用者於此商店的角色。</summary>
    /// <example>Owner</example>
    public StoreMemberRole Role { get; set; }
}

/// <summary>更新商店資料請求（部分欄位，未提供者不變更）。</summary>
public class UpdateStoreRequest
{
    /// <summary>商店顯示名稱；null 表示不變更。</summary>
    /// <example>小明的數位商店</example>
    public string? StoreName { get; set; }

    /// <summary>商店描述；null 表示不變更，空字串表示清空。</summary>
    /// <example>專注於數位插畫與素材販售。</example>
    public string? Description { get; set; }
}

/// <summary>申請 Avatar/Banner 上傳簽章 URL 請求。</summary>
public class RequestAssetUploadUrlRequest
{
    /// <summary>原始檔名（含副檔名）。</summary>
    /// <example>avatar.png</example>
    public string FileName { get; set; } = "";

    /// <summary>MIME 類型，僅允許 jpeg/png/gif/webp。</summary>
    /// <example>image/png</example>
    public string ContentType { get; set; } = "";

    /// <summary>檔案大小（bytes）。</summary>
    /// <example>204800</example>
    public long SizeBytes { get; set; }
}

/// <summary>確認 Avatar/Banner 已上傳完成的請求。</summary>
public class ConfirmAssetUploadRequest
{
    /// <summary>欲確認的 Asset ID（由上傳簽章 URL 回應取得）。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid AssetId { get; set; }
}

/// <summary>Avatar/Banner 上傳簽章 URL 回應。</summary>
public class AssetUploadUrlResponse
{
    /// <summary>已建立的 Asset ID。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid AssetId { get; set; }

    /// <summary>前端應使用此 URL 以 HTTP PUT 直傳檔案。</summary>
    /// <example>http://localhost:5171/v1/files/blob/public/.../avatar.png?expires=1735689600&amp;sig=...</example>
    public string UploadUrl { get; set; } = "";

    /// <summary>上傳完成後的公開讀取網址。</summary>
    /// <example>http://localhost:5171/v1/files/blob/public/.../avatar.png</example>
    public string PublicUrl { get; set; } = "";

    /// <summary>簽章 URL 過期時間（UTC）。</summary>
    public DateTimeOffset ExpiresAt { get; set; }
}
