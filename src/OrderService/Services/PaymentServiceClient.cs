using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Shared.Auth;
using Shared.Exceptions;

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

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);

            // 業務性錯誤（如商店尚未完成收款設定）原樣轉給前端顯示，不再退化成不透明的 500。
            if (response.StatusCode == HttpStatusCode.UnprocessableEntity)
                throw new ValidationException(ExtractDetail(body) ?? "無法建立付款，商店可能尚未完成收款設定。");

            // 其餘（Stripe 例外致 500、服務認證 401/403 等）：帶上 PaymentService 回應內容拋出，
            // 讓 OrderService 的例外記錄能看到實際失敗原因，而非僅一個裸露的狀態碼。
            throw new HttpRequestException(
                $"PaymentService checkout-session 回應 {(int)response.StatusCode}：{body}");
        }

        return await response.Content.ReadFromJsonAsync<CheckoutSessionResult>(cancellationToken: ct)
            ?? throw new HttpRequestException("PaymentService 回應內容為空。");
    }

    /// <summary>從 RFC 9457 Problem Details 回應中取出 <c>detail</c> 文字；解析失敗回 null。</summary>
    private static string? ExtractDetail(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;

        try
        {
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.TryGetProperty("detail", out var detail)
                ? detail.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>PaymentService <c>POST /v1/payments/checkout-session</c> 回應（僅取用所需欄位）。</summary>
    public class CheckoutSessionResult
    {
        public Guid PaymentId { get; set; }

        public string Url { get; set; } = "";
    }
}
