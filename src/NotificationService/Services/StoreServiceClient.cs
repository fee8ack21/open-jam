using System.Net.Http.Json;
using Shared.Exceptions;

namespace NotificationService.Services;

/// <summary>
/// 呼叫 StoreService 的客戶端：驗證商店擁有權（轉發呼叫者 Bearer token 至 <c>GET /v1/stores/me</c>），
/// 與查詢商店公開資訊（<c>GET /v1/stores/{id}</c>，匿名端點，dispatcher 渲染通知用）。
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

    /// <summary>查詢商店公開資訊（名稱與子網域代稱）。找不到拋 <see cref="NotFoundException"/>。</summary>
    /// <param name="storeId">商店 ID。</param>
    /// <param name="ct">Cancellation token。</param>
    public async Task<StoreInfo> GetStoreAsync(Guid storeId, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("store");

        using var response = await client.GetAsync($"v1/stores/{storeId}", ct);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            throw new NotFoundException("找不到商店。");

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<StoreInfo>(cancellationToken: ct)
            ?? throw new InvalidOperationException("StoreService 回應內容為空。");
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

    /// <summary>商店公開資訊（僅取用渲染通知所需欄位）。</summary>
    public class StoreInfo
    {
        /// <summary>商店 ID。</summary>
        public Guid Id { get; set; }

        /// <summary>商店顯示名稱。</summary>
        public string StoreName { get; set; } = "";

        /// <summary>商店子網域代稱。</summary>
        public string StoreSlug { get; set; } = "";
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
