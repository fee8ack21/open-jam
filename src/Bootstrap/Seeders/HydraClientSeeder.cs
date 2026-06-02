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
        await EnsureClientAsync(BuildSpaClient());
        await EnsureClientAsync(BuildApiClient());
    }

    private HydraOAuth2Client BuildSpaClient() => new()
    {
        ClientId                = config["HydraClients:Spa:ClientId"] ?? "open-jam-spa",
        ClientName              = "Open Jam SPA",
        GrantTypes              = ["authorization_code", "refresh_token"],
        ResponseTypes           = ["code"],
        Scope                   = "openid profile email offline_access",
        RedirectUris            = config.GetSection("HydraClients:Spa:RedirectUris").Get<string[]>()
                                      ?? ["http://localhost:3000/callback"],
        PostLogoutRedirectUris  = config.GetSection("HydraClients:Spa:PostLogoutRedirectUris").Get<string[]>()
                                      ?? ["http://localhost:3000"],
        TokenEndpointAuthMethod = "none",
    };

    private HydraOAuth2Client BuildApiClient() => new()
    {
        ClientId                = config["HydraClients:Api:ClientId"] ?? "open-jam-api",
        ClientName              = "Open Jam API",
        ClientSecret            = config["HydraClients:Api:ClientSecret"] ?? "change-me-in-production",
        GrantTypes              = ["client_credentials"],
        ResponseTypes           = [],
        Scope                   = "openid",
        TokenEndpointAuthMethod = "client_secret_basic",
    };

    private async Task EnsureClientAsync(HydraOAuth2Client client)
    {
        var check = await _http.GetAsync($"admin/clients/{Uri.EscapeDataString(client.ClientId)}");
        if (check.StatusCode == HttpStatusCode.OK)
        {
            logger.LogInformation("Hydra client '{ClientId}' already exists, skipping", client.ClientId);
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
