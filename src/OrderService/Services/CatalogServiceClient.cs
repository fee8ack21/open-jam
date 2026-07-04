using System.Net;
using System.Net.Http.Json;

namespace OrderService.Services;

/// <summary>
/// 呼叫 CatalogService 取得商品公開資訊的客戶端，供結帳時伺服器端核價。
/// 一律匿名呼叫 <c>GET /v1/catalogs/{id}</c>（不轉發呼叫者 token）——
/// 未上架 / 不存在的商品該端點對匿名回 404，天然排除不可購買的商品。
/// </summary>
public class CatalogServiceClient(IHttpClientFactory httpClientFactory)
{
    /// <summary>查詢商品公開資訊；商品不存在或未上架時回傳 null。</summary>
    /// <param name="catalogId">商品 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    public async Task<CatalogInfo?> GetCatalogAsync(Guid catalogId, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("catalog");

        using var response = await client.GetAsync($"v1/catalogs/{catalogId}", ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<CatalogInfo>(cancellationToken: ct)
            ?? throw new HttpRequestException("CatalogService 回應內容為空。");
    }

    /// <summary>CatalogService <c>GET /v1/catalogs/{id}</c> 回應（僅取用核價所需欄位）。</summary>
    public class CatalogInfo
    {
        public Guid Id { get; set; }

        public Guid StoreId { get; set; }

        public string Name { get; set; } = "";

        public decimal Price { get; set; }

        public string Currency { get; set; } = "";

        public CatalogVersionInfo? CurrentVersion { get; set; }
    }

    /// <summary>商品目前版本參照（僅含下單快照所需欄位）。</summary>
    public class CatalogVersionInfo
    {
        public Guid Id { get; set; }
    }
}
