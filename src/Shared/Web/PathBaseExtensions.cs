using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace Shared.Web;

/// <summary>
/// 提供平台共用的 PathBase 設定。正式環境各 REST API 服務以 path 前綴掛在
/// <c>api.openjam.co</c> 之下（如 <c>/store-service</c> → StoreService）；GKE 原生
/// GCE Ingress 不會剝除該前綴，故由各服務於入站時自行剝除，讓 Controller 路由
/// （<c>/v1/...</c>）得以正確比對。
/// </summary>
public static class PathBaseExtensions
{
    /// <summary>
    /// 依設定 <c>PathBase</c> 套用 <see cref="UsePathBaseExtensions.UsePathBase"/>，
    /// 將請求前綴（如 <c>/store-service</c>）移入 <c>Request.PathBase</c>。
    /// 須為管線中第一個 middleware，使後續所有環節（例外處理、路由、CORS）皆見到
    /// 已校正的路徑。未設定 <c>PathBase</c>（如本機開發）時不作任何處理。
    /// 不以前綴開頭的請求（如健康檢查 <c>/healthz</c>）原樣通過，不受影響。
    /// </summary>
    public static WebApplication UseOpenJamPathBase(this WebApplication app)
    {
        var pathBase = app.Configuration["PathBase"];
        if (!string.IsNullOrEmpty(pathBase))
            app.UsePathBase(pathBase);

        return app;
    }
}
