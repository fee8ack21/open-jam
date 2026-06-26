using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;

namespace CatalogService.Services;

/// <summary>
/// 呼叫 OrderService 驗證購買的客戶端。
/// 轉發呼叫者的 Bearer token 至 <c>GET /v1/orders/purchased/{catalogId}</c>，據此判斷是否已購買。
/// </summary>
public class OrderServiceClient(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
{
    /// <summary>查詢目前使用者是否已購買（已完成訂單）指定商品。</summary>
    /// <param name="catalogId">商品 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    public async Task<bool> HasPurchasedAsync(Guid catalogId, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("order");

        using var request = new HttpRequestMessage(HttpMethod.Get, $"v1/orders/purchased/{catalogId}");

        // 轉發呼叫者的 Authorization header，讓 OrderService 以同一身分判斷購買紀錄。
        var authorization = httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrEmpty(authorization))
            request.Headers.TryAddWithoutValidation("Authorization", authorization);

        using var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PurchaseCheckResult>(cancellationToken: ct);
        return result?.Purchased ?? false;
    }

    /// <summary>OrderService 購買驗證回應。</summary>
    private class PurchaseCheckResult
    {
        public bool Purchased { get; set; }
    }
}
