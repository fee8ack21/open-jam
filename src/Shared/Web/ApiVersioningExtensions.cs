using Asp.Versioning;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Web;

/// <summary>
/// 提供平台共用的 API 版本設定。以 URL 區段標示版本（前綴 <c>v1</c>，如
/// <c>/v1/stores</c>），未指定時套用預設版本，並於回應標頭回報支援的版本。
/// </summary>
public static class ApiVersioningExtensions
{
    /// <summary>
    /// 加入 URL segment API 版本機制（前綴 <c>v1</c>）。需搭配 Controller 上的
    /// <c>[ApiVersion("1.0")]</c> 與路由 <c>[Route("v{version:apiVersion}/...")]</c> 使用。
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>預設版本 v1，未帶版本時自動套用，舊客戶端不致中斷。</item>
    /// <item><c>ReportApiVersions</c> 於回應加上 <c>api-supported-versions</c> 標頭。</item>
    /// <item>ApiExplorer 的 group name 與 URL 置換皆格式化為 <c>v1</c>（minor 為 0 時省略），
    /// 與 <c>SwaggerDoc("v1", ...)</c> 的文件名稱對應。</item>
    /// </list>
    /// </remarks>
    public static IServiceCollection AddOpenJamApiVersioning(this IServiceCollection services)
    {
        services
            .AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            .AddApiExplorer(options =>
            {
                // group name 對應 SwaggerDoc 名稱（v1, v2…）；VVV 在 minor 為 0 時省略 → "v1"
                options.GroupNameFormat = "'v'VVV";
                // 將路由的 {version:apiVersion} 置換為實際版本字串（1.0 → "1" → 前綴 v1）
                options.SubstituteApiVersionInUrl = true;
                options.SubstitutionFormat = "VVV";
            });

        return services;
    }
}
