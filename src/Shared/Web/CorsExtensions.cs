using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Web;

/// <summary>
/// 提供平台共用的 CORS 設定，允許前端 SPA（market-web / creator-web / workspace-web）
/// 以瀏覽器 fetch 帶 JWT Bearer token 跨來源呼叫 REST API。
/// </summary>
public static class CorsExtensions
{
    /// <summary>共用 CORS policy 名稱。</summary>
    public const string PolicyName = "OpenJamCors";

    /// <summary>
    /// 未提供設定時的預設允許來源：本機 Vite dev server（5173~5175）與正式環境前端網域。
    /// 正式環境的創作者店面為 <c>&lt;creator&gt;.openjam.co</c> 子網域，以萬用字元樣式比對。
    /// </summary>
    private static readonly string[] DefaultOrigins =
    [
        "http://localhost:5173",
        "http://localhost:5174",
        "http://localhost:5175",
        "https://openjam.co",
        "https://workspace.openjam.co",
        "https://*.openjam.co",
    ];

    /// <summary>
    /// 加入共用 CORS policy。允許的來源由設定 <c>Cors:AllowedOrigins</c>（字串陣列）提供，
    /// 未設定時採用 <see cref="DefaultOrigins"/>。來源支援萬用子網域樣式（如
    /// <c>https://*.openjam.co</c>），用以涵蓋動態的創作者子網域。
    /// 前端以 Authorization header 帶 Bearer token（非 cookie），故不啟用 AllowCredentials。
    /// </summary>
    public static IServiceCollection AddOpenJamCors(this IServiceCollection services, IConfiguration config)
    {
        var origins = config.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (origins is null || origins.Length == 0)
            origins = DefaultOrigins;

        services.AddCors(options =>
            options.AddPolicy(PolicyName, policy => policy
                .SetIsOriginAllowed(origin => IsOriginAllowed(origin, origins))
                .AllowAnyHeader()
                .AllowAnyMethod()));

        return services;
    }

    /// <summary>
    /// 套用共用 CORS policy。需排在 UseAuthentication / UseAuthorization 之前。
    /// </summary>
    public static IApplicationBuilder UseOpenJamCors(this IApplicationBuilder app)
        => app.UseCors(PolicyName);

    /// <summary>
    /// 判斷請求來源是否符合任一允許樣式。樣式以 <c>*.</c> 開頭的主機名表示萬用子網域，
    /// 比對該網域本身與其任意子網域；其餘為完全比對。
    /// </summary>
    private static bool IsOriginAllowed(string origin, string[] patterns)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
            return false;

        foreach (var pattern in patterns)
        {
            if (!Uri.TryCreate(pattern, UriKind.Absolute, out var allowed))
                continue;

            if (!string.Equals(uri.Scheme, allowed.Scheme, StringComparison.OrdinalIgnoreCase))
                continue;
            if (uri.Port != allowed.Port)
                continue;

            var allowedHost = allowed.Host;
            if (allowedHost.StartsWith("*.", StringComparison.Ordinal))
            {
                // 萬用子網域：比對基底網域本身（example.com）與其子網域（a.example.com）
                var baseHost = allowedHost[2..];
                if (string.Equals(uri.Host, baseHost, StringComparison.OrdinalIgnoreCase) ||
                    uri.Host.EndsWith("." + baseHost, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            else if (string.Equals(uri.Host, allowedHost, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
