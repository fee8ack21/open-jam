using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Shared.Exceptions;

namespace CatalogService.Services;

/// <summary>
/// 呼叫 QuotaService 進行儲存空間預扣 / 釋放與上架商品數計數的客戶端。
/// 轉發呼叫者的 Bearer token，讓 QuotaService 以同一身分（JWT sub = 租戶）判斷配額。
/// </summary>
public class QuotaServiceClient(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
{
    /// <summary>預扣指定位元組數，回傳預扣紀錄 ID（由本端產生作為冪等鍵）。配額不足拋 409、超單檔 / 單商品上限拋 422。</summary>
    /// <param name="sizeBytes">欲預扣的位元組數。</param>
    /// <param name="productId">關聯商品 ID（單商品總量上限用）。</param>
    /// <param name="ct">Cancellation token。</param>
    public async Task<Guid> ReserveAsync(long sizeBytes, Guid productId, CancellationToken ct)
    {
        var reservationId = Guid.NewGuid();

        using var request = Build(HttpMethod.Post, "v1/reservations");
        request.Content = JsonContent.Create(new
        {
            ReservationId = reservationId,
            Size = sizeBytes,
            ProductId = (Guid?)productId,
        });

        using var response = await Send(request, ct);
        await EnsureQuotaSuccessAsync(response, ct);

        return reservationId;
    }

    /// <summary>釋放預扣（簽章失敗等情境）。best-effort：失敗不拋出，避免遮蔽原始錯誤。</summary>
    /// <param name="reservationId">預扣紀錄 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    public async Task ReleaseAsync(Guid reservationId, CancellationToken ct)
    {
        try
        {
            using var request = Build(HttpMethod.Post, $"v1/reservations/{reservationId}/release");
            using var response = await Send(request, ct);
        }
        catch
        {
            // 釋放為盡力而為；逾時未 commit 的預扣最終由 QuotaService sweeper 回收。
        }
    }

    /// <summary>增減上架商品數（+1 進入 Published、-1 離開 Published）。超上限拋 409。</summary>
    /// <param name="delta">增減量。</param>
    /// <param name="ct">Cancellation token。</param>
    public async Task ChangeProductCountAsync(int delta, CancellationToken ct)
    {
        using var request = Build(HttpMethod.Post, "v1/products/count");
        request.Content = JsonContent.Create(new { Delta = delta });

        using var response = await Send(request, ct);
        await EnsureQuotaSuccessAsync(response, ct);
    }

    private HttpRequestMessage Build(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, path);

        var authorization = httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrEmpty(authorization))
            request.Headers.TryAddWithoutValidation("Authorization", authorization);

        return request;
    }

    private Task<HttpResponseMessage> Send(HttpRequestMessage request, CancellationToken ct) =>
        httpClientFactory.CreateClient("quota").SendAsync(request, ct);

    /// <summary>將 QuotaService 的配額錯誤狀態碼轉為對應 AppException，維持給前端的契約。</summary>
    private static async Task EnsureQuotaSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
            return;

        var detail = await ReadDetailAsync(response, ct);

        throw response.StatusCode switch
        {
            HttpStatusCode.Conflict => new ConflictException(detail ?? "配額不足。"),
            HttpStatusCode.UnprocessableEntity => new ValidationException(detail ?? "超出配額上限。"),
            _ => new Exception($"QuotaService 回應非預期狀態：{(int)response.StatusCode}"),
        };
    }

    private static async Task<string?> ReadDetailAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetailLike>(ct);
            return problem?.Detail;
        }
        catch
        {
            return null;
        }
    }

    private class ProblemDetailLike
    {
        public string? Detail { get; set; }
    }
}
