namespace StoreService.Models;

/// <summary>駁回開店申請請求。</summary>
public class RejectStoreApplicationRequest
{
    /// <summary>駁回原因，將附於通知信中。</summary>
    /// <example>商店名稱與既有品牌過於相似，請調整後重新申請。</example>
    public string ReviewComment { get; set; } = "";
}
