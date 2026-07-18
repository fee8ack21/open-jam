using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Auth;

/// <summary>
/// 服務間（service-to-service）認證的 DI 整合。
/// 呼叫端註冊 <see cref="AddOpenJamServiceTokenClient"/> 取得 service token；
/// 被呼叫端註冊 <see cref="AddOpenJamInternalServicePolicy"/> 以 <c>"InternalService"</c> policy 限制僅內部服務可呼叫。
/// </summary>
public static class ServiceAuthExtensions
{
    /// <summary>
    /// 註冊 <see cref="ServiceTokenClient"/>（singleton）與其 HttpClient，
    /// 設定取自 <c>ServiceAuth</c> 區段（<c>TokenUrl</c> / <c>ClientId</c> / <c>ClientSecret</c>）。
    /// </summary>
    /// <param name="services">DI 服務容器。</param>
    /// <param name="config">應用程式設定。</param>
    public static IServiceCollection AddOpenJamServiceTokenClient(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<ServiceAuthOptions>(config.GetSection("ServiceAuth"));
        services.AddHttpClient("service-auth");
        services.AddSingleton<ServiceTokenClient>();
        // 供呼叫端以 AddHttpMessageHandler<ServiceTokenHandler>() 掛在需認證的 named client 上。
        services.AddTransient<ServiceTokenHandler>();
        return services;
    }

    /// <summary>
    /// 加入 <c>"InternalService"</c> 授權 policy：要求 JWT 的 <c>sub</c> claim 等於服務用 client id
    /// （<c>ServiceAuth:ClientId</c>，預設 <c>open-jam-service</c>；Hydra client_credentials token 的 sub 即 client id）。
    /// 需先呼叫 <c>AddOpenJamJwtAuth</c> 完成 JwtBearer 驗證設定。
    /// </summary>
    /// <param name="services">DI 服務容器。</param>
    /// <param name="config">應用程式設定。</param>
    public static IServiceCollection AddOpenJamInternalServicePolicy(this IServiceCollection services, IConfiguration config)
    {
        var clientId = config["ServiceAuth:ClientId"] ?? "open-jam-service";

        services.AddAuthorizationBuilder()
            .AddPolicy("InternalService", policy => policy.RequireClaim("sub", clientId));

        return services;
    }
}
