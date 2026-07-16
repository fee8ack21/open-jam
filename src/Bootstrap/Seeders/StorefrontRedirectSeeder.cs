using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Auth.Services.Storefront;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StoreService.Data;

namespace Bootstrap.Seeders;

/// <summary>
/// 一次性回填 Hydra web client 的店面子網域 redirect URI：
/// 讀取 StoreService 既有商店 slug（StoreProvisionedEvent 上線前核准的存量店家），
/// 缺漏的 callback / silent-renew / post-logout URI 才追加，重跑冪等。
/// 新店家由 Auth 消費 StoreProvisionedEvent 即時註冊，不經此 seeder。
/// </summary>
public class StorefrontRedirectSeeder(
    StoreDbContext storeDb,
    IHttpClientFactory factory,
    IConfiguration config,
    ILogger<StorefrontRedirectSeeder> logger)
{
    private readonly HttpClient _http = factory.CreateClient("hydra");

    public async Task SeedAsync()
    {
        var clientId = config["HydraClients:Web:ClientId"] ?? "open-jam-web";
        var pattern  = config["HydraClients:Web:StorefrontUrlPattern"]
                       ?? StorefrontRedirectUris.DefaultUrlPattern;

        var slugs = await storeDb.Stores.AsNoTracking()
            .Select(s => s.StoreSlug)
            .ToListAsync();

        if (slugs.Count == 0)
        {
            logger.LogInformation("Storefront redirect URIs：無既有商店，略過");
            return;
        }

        var response = await _http.GetAsync($"admin/clients/{Uri.EscapeDataString(clientId)}");
        response.EnsureSuccessStatusCode();
        var client = (await response.Content.ReadFromJsonAsync<JsonNode>())!;

        var mergedCount = slugs.Count(slug => StorefrontRedirectUris.MergeInto(client, pattern, slug));

        if (mergedCount > 0)
        {
            var put = await _http.PutAsJsonAsync($"admin/clients/{Uri.EscapeDataString(clientId)}", client);
            put.EnsureSuccessStatusCode();
        }

        logger.LogInformation(
            "Storefront redirect URIs seeded：補入 {Merged} 家店的 URI（商店共 {Total} 家）", mergedCount, slugs.Count);
    }
}
