using System.Net.Http.Json;

namespace QuotaService.Services;

/// <summary>呼叫 StorageService 取得租戶實際用量（每日對帳校正計數器漂移用）。</summary>
public class StorageServiceClient(IHttpClientFactory httpClientFactory)
{
    /// <summary>取得指定創作者已 ready 檔案的位元組總和。</summary>
    /// <param name="creatorId">創作者（租戶）ID。</param>
    /// <param name="ct">Cancellation token。</param>
    public async Task<long> GetTenantUsageBytesAsync(Guid creatorId, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("storage");

        var response = await client.GetAsync($"v1/files/usage?creatorId={creatorId}", ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<StorageUsageResult>(ct);
        return result?.TotalBytes ?? 0;
    }
}

/// <summary>StorageService <c>GET /v1/files/usage</c> 回應。</summary>
public class StorageUsageResult
{
    /// <summary>該租戶已 ready 檔案的位元組總和。</summary>
    public long TotalBytes { get; set; }
}
