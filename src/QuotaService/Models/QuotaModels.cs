using QuotaService.Data.Entities;

namespace QuotaService.Models;

/// <summary>建立儲存空間預扣的請求。於簽發上傳 signed URL 之前由功能 API 呼叫。</summary>
public class ReserveRequest
{
    /// <summary>預扣紀錄 ID，由呼叫方產生並作為冪等鍵；同 ID 重送回傳既有結果，不重複預扣。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid ReservationId { get; set; }

    /// <summary>欲預扣的位元組數。</summary>
    /// <example>104857600</example>
    public long Size { get; set; }

    /// <summary>關聯的商品 ID；提供時一併做單商品總量上限檢查；null 表示尚未關聯。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid? ProductId { get; set; }
}

/// <summary>預扣結果。</summary>
public class ReservationResponse
{
    /// <summary>預扣紀錄 ID。</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid ReservationId { get; set; }

    /// <summary>預扣狀態。</summary>
    /// <example>Reserved</example>
    public ReservationStatus Status { get; set; }

    /// <summary>預扣有效期（UTC）。</summary>
    public DateTimeOffset ExpiresAt { get; set; }
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
