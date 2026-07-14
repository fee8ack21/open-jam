using System.Net.Http.Headers;
using System.Net.Http.Json;
using Shared.Auth;

namespace OrderService.Services;

/// <summary>
/// 呼叫 PaymentService 建立 Stripe Checkout Session 的客戶端。
/// 結帳時由 OrderService 於建單後 server-to-server 呼叫，將付款頁 URL 一併回傳給前端。
/// 以 <see cref="ServiceTokenClient"/> 取得的 service token 認證（PaymentService 端點僅限內部服務呼叫）。
/// </summary>
public class PaymentServiceClient(IHttpClientFactory httpClientFactory, ServiceTokenClient serviceToken)
{
    /// <summary>為指定訂單建立（或重用）Checkout Session，回傳 Stripe 付款頁 URL。</summary>
    /// <param name="orderId">訂單 ID。</param>
    /// <param name="storeId">賣方商店 ID（PaymentService 據此查找分帳目的地帳戶）。</param>
    /// <param name="buyerUserId">購買者使用者 ID；null 表示匿名購買。</param>
    /// <param name="email">購買者電子信箱。</param>
    /// <param name="amount">付款金額（最低貨幣單位，如 cents）。</param>
    /// <param name="currency">貨幣代碼（小寫，如 "usd"）。</param>
    /// <param name="productName">顯示於 Stripe Checkout 頁面的商品名稱。</param>
    /// <param name="ct">Cancellation token。</param>
    public async Task<CheckoutSessionResult> CreateCheckoutSessionAsync(
        Guid orderId, Guid storeId, Guid? buyerUserId, string email, long amount, string currency,
        string productName, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("payment");

        using var request = new HttpRequestMessage(HttpMethod.Post, "v1/payments/checkout-session")
        {
            Content = JsonContent.Create(new
            {
                OrderId = orderId,
                StoreId = storeId,
                UserId = buyerUserId,
                Email = email,
                Amount = amount,
                Currency = currency,
                ProductName = productName,
            }),
        };

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", await serviceToken.GetTokenAsync(ct));

        using var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<CheckoutSessionResult>(cancellationToken: ct)
            ?? throw new HttpRequestException("PaymentService 回應內容為空。");
    }

    /// <summary>PaymentService <c>POST /v1/payments/checkout-session</c> 回應（僅取用所需欄位）。</summary>
    public class CheckoutSessionResult
    {
        public Guid PaymentId { get; set; }

        public string Url { get; set; } = "";
    }
}
