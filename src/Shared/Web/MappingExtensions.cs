using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Web;

/// <summary>
/// 平台共用的 AutoMapper 整合。各 REST API service 於 <c>Program.cs</c> 呼叫
/// <see cref="AddOpenJamMapping"/> 註冊 assembly 內的所有 <c>Profile</c>，
/// 業務層注入 <c>IMapper</c> 進行 Entity ↔ DTO 的扁平欄位轉換；需 async 查詢補值的欄位
/// （如資產 URL、標籤清單）仍由 service 在 map 後補上。
/// </summary>
public static class MappingExtensions
{
    /// <summary>註冊指定 assembly 內的所有 AutoMapper <c>Profile</c>。</summary>
    /// <param name="services">服務容器。</param>
    /// <param name="assembly">放置 Profile 的 assembly（通常為 service 進入點 assembly）。</param>
    public static IServiceCollection AddOpenJamMapping(this IServiceCollection services, Assembly assembly)
    {
        services.AddAutoMapper(assembly);
        return services;
    }
}
