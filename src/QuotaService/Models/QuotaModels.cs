namespace QuotaService.Models;

/// <summary>儲存空間扣量請求。使用者提交確認、功能 API 建立檔案 reference 時呼叫（上傳完成後）。</summary>
public class ChargeRequest
{
    /// <summary>扣量紀錄 ID，由呼叫方產生並作為冪等鍵（慣例帶入檔案 ID）；同 ID 重送不重複扣量。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid ChargeId { get; set; }

    /// <summary>實際使用的位元組數（上傳完成後的實際檔案大小）。</summary>
    /// <example>104857600</example>
    public long Size { get; set; }

    /// <summary>關聯的商品 ID；提供時一併做單商品總量上限檢查；null 表示尚未關聯。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid? ProductId { get; set; }
}

/// <summary>商品數增減請求（CatalogService 於商品上 / 下架時呼叫）。</summary>
public class ChangeProductCountRequest
{
    /// <summary>增減量；進入 Published 為 +1，離開 Published 為 -1。</summary>
    /// <example>1</example>
    public int Delta { get; set; }
}

/// <summary>租戶用量回應。</summary>
public class UsageResponse
{
    /// <summary>租戶（創作者）ID。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid TenantId { get; set; }

    /// <summary>儲存空間配額上限（bytes）。</summary>
    /// <example>53687091200</example>
    public long Quota { get; set; }

    /// <summary>已實際使用（bytes）。</summary>
    /// <example>1048576</example>
    public long Used { get; set; }

    /// <summary>已預扣未 commit（bytes）。</summary>
    /// <example>0</example>
    public long Reserved { get; set; }

    /// <summary>可用餘額（bytes）= Quota - Used - Reserved。</summary>
    /// <example>53686042624</example>
    public long Available => Math.Max(Quota - Used - Reserved, 0);

    /// <summary>目前上架商品數。</summary>
    /// <example>3</example>
    public int ProductCount { get; set; }

    /// <summary>上架商品數上限。</summary>
    /// <example>100</example>
    public int MaxProducts { get; set; }
}
