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
