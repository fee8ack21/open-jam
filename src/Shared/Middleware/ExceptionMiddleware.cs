using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shared.Exceptions;

namespace Shared.Middleware;

/// <summary>
/// 全域例外攔截 Middleware，將 AppException 及未預期例外統一轉換為 RFC 9457 Problem Details 格式。
/// 在 Program.cs 以 app.UseExceptionMiddleware() 掛載，需排在所有其他 middleware 之前。
/// </summary>
public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    /// <inheritdoc/>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (AppException ex)
        {
            await WriteProblemAsync(context, ex.StatusCode, ex.ErrorCode, ex.Message,
                ex is ValidationException ve ? ve.Errors : null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            await WriteProblemAsync(context, 500, "InternalServerError", "發生非預期錯誤");
        }
    }

    private static async Task WriteProblemAsync(
        HttpContext context,
        int status,
        string errorCode,
        string detail,
        IReadOnlyDictionary<string, string[]>? errors = null)
    {
        context.Response.StatusCode  = status;
        context.Response.ContentType = "application/problem+json";

        var problem = new
        {
            type    = $"https://open-jam.dev/errors/{errorCode}",
            title   = errorCode,
            status,
            detail,
            errors,
            traceId = Activity.Current?.Id ?? context.TraceIdentifier,
        };

        await context.Response.WriteAsJsonAsync(problem);
    }
}

/// <summary>ExceptionMiddleware 的 IApplicationBuilder 擴充方法。</summary>
public static class ExceptionMiddlewareExtensions
{
    /// <summary>掛載 ExceptionMiddleware，應排在 pipeline 最前面。</summary>
    public static IApplicationBuilder UseExceptionMiddleware(this IApplicationBuilder app)
        => app.UseMiddleware<ExceptionMiddleware>();
}
