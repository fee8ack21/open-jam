using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Bootstrap.Seeders;

public class HydraClientSeeder(
    IHttpClientFactory factory,
    IConfiguration config,
    ILogger<HydraClientSeeder> logger)
{
    private readonly HttpClient _http = factory.CreateClient("hydra");

    public async Task SeedAsync()
    {
        // Web client：已存在則略過——其 redirect_uris 由 Auth 消費 StoreProvisionedEvent 動態逐店追加
        // （StorefrontRedirectService），整包覆寫會抹掉這些動態註冊，故不 force update。
        await EnsureClientAsync(BuildWebClient(), forceUpdate: false);
        // Service client：純靜態（client_credentials，無動態狀態），已存在時整包覆寫使其收斂到目前設定，
        // 避免密鑰輪替後 Hydra 端仍留舊 secret 導致服務間換 token 回 401。
        await EnsureClientAsync(BuildServiceClient(), forceUpdate: true);
    }

    private HydraOAuth2Client BuildWebClient() => new()
    {
        ClientId                = config["HydraClients:Web:ClientId"] ?? "open-jam-web",
        ClientName              = "Open Jam Web",
        GrantTypes              = ["authorization_code", "refresh_token"],
        ResponseTypes           = ["code"],
        Scope                   = "openid profile email offline_access",
        RedirectUris            = config.GetSection("HydraClients:Web:RedirectUris").Get<string[]>()
                                      ?? ["http://localhost:3000/callback"],
        PostLogoutRedirectUris  = config.GetSection("HydraClients:Web:PostLogoutRedirectUris").Get<string[]>()
                                      ?? ["http://localhost:3000"],
        TokenEndpointAuthMethod = "none",
    };

    private HydraOAuth2Client BuildServiceClient() => new()
    {
        ClientId                = config["HydraClients:Service:ClientId"] ?? "open-jam-service",
        ClientName              = "Open Jam Service",
        ClientSecret            = config["HydraClients:Service:ClientSecret"] ?? "change-me-in-production",
        GrantTypes              = ["client_credentials"],
        ResponseTypes           = [],
        Scope                   = "openid",
        TokenEndpointAuthMethod = "client_secret_basic",
    };

    private async Task EnsureClientAsync(HydraOAuth2Client client, bool forceUpdate)
    {
        var check = await _http.GetAsync($"admin/clients/{Uri.EscapeDataString(client.ClientId)}");
        if (check.StatusCode == HttpStatusCode.OK)
        {
            if (!forceUpdate)
            {
                logger.LogInformation("Hydra client '{ClientId}' already exists, skipping", client.ClientId);
                return;
            }

            // 整包覆寫（含 client_secret），使既有 client 收斂到目前設定值。
            var update = await _http.PutAsJsonAsync(
                $"admin/clients/{Uri.EscapeDataString(client.ClientId)}", client);
            update.EnsureSuccessStatusCode();
            logger.LogInformation("Updated Hydra client '{ClientId}'", client.ClientId);
            return;
        }

        var response = await _http.PostAsJsonAsync("admin/clients", client);
        response.EnsureSuccessStatusCode();
        logger.LogInformation("Created Hydra client '{ClientId}'", client.ClientId);
    }
}

internal record HydraOAuth2Client
{
    [JsonPropertyName("client_id")]                  public string   ClientId                { get; init; } = "";
    [JsonPropertyName("client_name")]                public string?  ClientName              { get; init; }
    [JsonPropertyName("client_secret")]              public string?  ClientSecret            { get; init; }
    [JsonPropertyName("grant_types")]                public string[] GrantTypes              { get; init; } = [];
    [JsonPropertyName("response_types")]             public string[] ResponseTypes           { get; init; } = [];
    [JsonPropertyName("scope")]                      public string   Scope                   { get; init; } = "";
    [JsonPropertyName("redirect_uris")]              public string[] RedirectUris            { get; init; } = [];
    [JsonPropertyName("post_logout_redirect_uris")]  public string[] PostLogoutRedirectUris  { get; init; } = [];
    [JsonPropertyName("token_endpoint_auth_method")] public string   TokenEndpointAuthMethod { get; init; } = "none";
}
