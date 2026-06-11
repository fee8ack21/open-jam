using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Auth;

/// <summary>
/// 提供平台共用的 JWT Bearer 驗證設定，驗證對象為 Hydra 簽發的 JWT access token。
/// </summary>
public static class JwtBearerExtensions
{
    /// <summary>
    /// 加入 JwtBearer 驗證（驗證 Hydra JWKS）與 "Admin" 授權 policy。
    /// </summary>
    /// <param name="services">DI 服務容器。</param>
    /// <param name="config">
    /// 應用程式設定，需包含 "Hydra:Issuer"（須與 token 的 iss claim 完全一致）；
    /// 選填 "Hydra:MetadataAddress"（容器網路中可達的 OIDC discovery URL，
    /// 預設與 Issuer 相同，僅當 Issuer 對外位址在容器內不可達時才需另外指定）。
    /// </param>
    public static IServiceCollection AddOpenJamJwtAuth(this IServiceCollection services, IConfiguration config)
    {
        var issuer = config["Hydra:Issuer"]
            ?? throw new InvalidOperationException("設定缺失：Hydra:Issuer");
        var metadataAddress = config["Hydra:MetadataAddress"];

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = issuer;
                if (!string.IsNullOrEmpty(metadataAddress))
                    options.MetadataAddress = metadataAddress;
                options.RequireHttpsMetadata = issuer.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
                options.MapInboundClaims = false;
                options.TokenValidationParameters.ValidIssuer = issuer;
                options.TokenValidationParameters.ValidateAudience = false;

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        var principal = context.Principal;
                        var identity  = principal?.Identity as ClaimsIdentity;
                        var ext       = principal?.FindFirst("ext")?.Value;

                        if (identity is not null && !string.IsNullOrEmpty(ext))
                        {
                            using var doc = JsonDocument.Parse(ext);
                            if (doc.RootElement.TryGetProperty("role", out var role) && role.ValueKind == JsonValueKind.String)
                                identity.AddClaim(new Claim(ClaimTypes.Role, role.GetString()!));
                            if (doc.RootElement.TryGetProperty("email", out var email) && email.ValueKind == JsonValueKind.String)
                                identity.AddClaim(new Claim(ClaimTypes.Email, email.GetString()!));
                        }

                        return Task.CompletedTask;
                    },
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy("Admin", policy => policy.RequireRole("Admin"));

        return services;
    }
}
