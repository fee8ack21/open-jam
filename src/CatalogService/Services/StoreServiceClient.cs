using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Shared.Exceptions;

namespace CatalogService.Services;

/// <summary>
/// 呼叫 StoreService 驗證商店擁有權的客戶端。
/// 轉發呼叫者的 Bearer token 至 StoreService 的 <c>GET /v1/stores/me</c>，據此判斷是否為店家 Owner。
/// </summary>
public class StoreServiceClient(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
{
    /// <summary>確認目前使用者為該商店的 Owner，否則拋出 <see cref="ForbiddenException"/>。</summary>
    /// <param name="storeId">商店 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    public async Task EnsureStoreOwnerAsync(Guid storeId, CancellationToken ct)
    {
        var stores = await GetMyStoresAsync(ct);

        var isOwner = stores.Any(s =>
            s.Store.Id == storeId && string.Equals(s.Role, "Owner", StringComparison.OrdinalIgnoreCase));

        if (!isOwner)
            throw new ForbiddenException();
    }

    private async Task<List<MyStoreResult>> GetMyStoresAsync(CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("store");

        using var request = new HttpRequestMessage(HttpMethod.Get, "v1/stores/me");

        // 轉發呼叫者的 Authorization header，讓 StoreService 以同一身分判斷成員資格。
        var authorization = httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrEmpty(authorization))
            request.Headers.TryAddWithoutValidation("Authorization", authorization);

        using var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<MyStoreResult>>(cancellationToken: ct) ?? [];
    }

    /// <summary>StoreService <c>GET /v1/stores/me</c> 回應單筆（僅取用 Store.Id 與 Role）。</summary>
    private class MyStoreResult
    {
        public StoreRef Store { get; set; } = new();

        public string Role { get; set; } = "";
    }

    /// <summary>商店參照（僅含判斷所需欄位）。</summary>
    private class StoreRef
    {
        public Guid Id { get; set; }
    }
}
