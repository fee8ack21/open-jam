using System.Reflection;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Web;

/// <summary>
/// 提供平台共用的 Swagger / OpenAPI 文件設定，使各 REST API service 的
/// 產生規則一致：單一 <c>v1</c> 文件、納入 XML 註解，並以 Controller Action
/// 名稱作為 <c>operationId</c>。
/// </summary>
public static class SwaggerExtensions
{
    /// <summary>
    /// 加入 ApiExplorer 與 SwaggerGen，套用平台共用設定。
    /// </summary>
    /// <param name="services">服務容器。</param>
    /// <param name="title">Swagger 文件標題（通常為服務名稱）。</param>
    /// <remarks>
    /// <list type="bullet">
    /// <item>文件名稱固定為 <c>v1</c>，對應 <see cref="ApiVersioningExtensions"/> 的 group name。</item>
    /// <item>自動納入進入點組件的 XML 註解檔（<c>&lt;EntryAssembly&gt;.xml</c>，存在才載入）。</item>
    /// <item><c>operationId</c> 取 Controller Action 名稱（已去除 <c>Async</c> 後綴），
    /// 讓 swagger-typescript-api 產出的方法名與 .NET action 一致。</item>
    /// </list>
    /// </remarks>
    public static IServiceCollection AddOpenJamSwagger(this IServiceCollection services, string title)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(opts =>
        {
            opts.SwaggerDoc("v1", new() { Title = title, Version = "v1" });

            var xmlName = $"{Assembly.GetEntryAssembly()!.GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlName);
            if (File.Exists(xmlPath)) opts.IncludeXmlComments(xmlPath);

            opts.CustomOperationIds(api =>
                api.ActionDescriptor is ControllerActionDescriptor cad ? cad.ActionName : null);
        });

        return services;
    }
}
