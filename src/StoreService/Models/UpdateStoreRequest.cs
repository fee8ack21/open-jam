namespace StoreService.Models;

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
