using System.Net.Http.Json;
using Shared.Exceptions;

namespace CatalogService.Services;

/// <summary>
/// 呼叫 PaymentService 查詢商店收款狀態的客戶端。
/// 匿名查詢 <c>GET /v1/connect/accounts/{storeId}/status</c>（僅布林旗標），
/// 供付費商品上架 / 改價閘門判斷商店是否已完成 Stripe Connect onboarding。
/// </summary>
public class PaymentServiceClient(IHttpClientFactory httpClientFactory)
{
    /// <summary>確認商店已可承接款項（ChargesEnabled），否則拋出 422 阻止付費商品上架。</summary>
    /// <param name="storeId">商店 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    public async Task EnsurePayoutReadyAsync(Guid storeId, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("payment");

        using var response = await client.GetAsync($"v1/connect/accounts/{storeId}/status", ct);
        response.EnsureSuccessStatusCode();

        var status = await response.Content.ReadFromJsonAsync<ConnectStatusResult>(cancellationToken: ct);

        if (status is not { ChargesEnabled: true })
            throw new ValidationException("商店尚未完成收款設定，請先於後台完成 Stripe 收款設定後再上架付費商品。");
    }

    /// <summary>PaymentService 收款狀態回應（僅取用閘門判斷所需欄位）。</summary>
    private class ConnectStatusResult
    {
        public bool ChargesEnabled { get; set; }
    }
}
