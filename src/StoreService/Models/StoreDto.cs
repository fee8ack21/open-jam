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
    /// <example>http://localhost:9000/open-jam/public/3fa85f64-5717-4562-b3fc-2c963f66afa6/avatar.png</example>
    public string? AvatarUrl { get; set; }

    /// <summary>商店橫幅公開 URL；null 表示尚未設定。</summary>
    /// <example>http://localhost:9000/open-jam/public/3fa85f64-5717-4562-b3fc-2c963f66afa6/banner.png</example>
    public string? BannerUrl { get; set; }

    /// <summary>商店狀態。</summary>
    /// <example>Active</example>
    public StoreStatus Status { get; set; }

    /// <summary>建立時間。</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>最後更新時間。</summary>
    public DateTimeOffset? UpdatedAt { get; set; }
}
