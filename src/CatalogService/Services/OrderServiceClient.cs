using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;

namespace CatalogService.Services;

/// <summary>
/// 呼叫 OrderService 驗證購買的客戶端。
/// 登入買家：轉發呼叫者的 Bearer token 至 <c>GET /v1/orders/purchased/{catalogId}</c>，據此判斷是否已購買。
/// 訪客買家：匿名查詢 <c>GET /v1/orders/{id}</c>，驗證訂單已完成且包含指定商品。
/// </summary>
public class OrderServiceClient(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
{
    /// <summary>驗證指定訂單已完成（Completed）且包含指定商品，供訪客憑訂單 ID 下載授權。</summary>
    /// <param name="orderId">訂單 ID（作為不可猜測的下載憑證）。</param>
    /// <param name="catalogId">商品 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    public async Task<bool> OrderGrantsCatalogAsync(Guid orderId, Guid catalogId, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("order");

        // 匿名呼叫（不轉發 token）：訂單查詢端點對匿名開放，訂單 ID 本身即為授權憑證。
        using var response = await client.GetAsync($"v1/orders/{orderId}", ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return false;

        response.EnsureSuccessStatusCode();

        var order = await response.Content.ReadFromJsonAsync<OrderResult>(cancellationToken: ct);
        return order is not null
            && string.Equals(order.Status, "Completed", StringComparison.OrdinalIgnoreCase)
            && order.Items.Any(i => i.CatalogId == catalogId);
    }

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

    /// <summary>OrderService <c>GET /v1/orders/{id}</c> 回應（僅取用授權判斷所需欄位）。</summary>
    private class OrderResult
    {
        public string Status { get; set; } = "";

        public List<OrderItemResult> Items { get; set; } = [];
    }

    /// <summary>訂單項目（僅取用商品 ID）。</summary>
    private class OrderItemResult
    {
        public Guid CatalogId { get; set; }
    }
}
