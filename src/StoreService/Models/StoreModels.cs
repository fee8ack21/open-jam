using StoreService.Data.Entities;

namespace StoreService.Models;

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
