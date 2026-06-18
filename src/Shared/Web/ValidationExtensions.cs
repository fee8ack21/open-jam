using System.Reflection;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using ValidationException = Shared.Exceptions.ValidationException;

namespace Shared.Web;

/// <summary>
/// 平台共用的 FluentValidation 整合。各 REST API service 於 <c>Program.cs</c> 呼叫
/// <see cref="AddOpenJamValidation"/> 註冊 assembly 內的所有 <see cref="IValidator{T}"/>，
/// 並掛上 <see cref="ValidationActionFilter"/>：在 action 執行前驗證 model，失敗時拋出
/// <see cref="ValidationException"/>（422），交由 <c>ExceptionMiddleware</c> 統一轉為
/// RFC 9457 Problem Details（含欄位層級 <c>errors</c>）。
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    /// 註冊指定 assembly 內的所有 FluentValidation 驗證器，並掛上全域 model 驗證 filter。
    /// </summary>
    /// <param name="services">服務容器。</param>
    /// <param name="assembly">放置驗證器的 assembly（通常為 service 進入點 assembly）。</param>
    public static IServiceCollection AddOpenJamValidation(this IServiceCollection services, Assembly assembly)
    {
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);
        services.Configure<MvcOptions>(opts => opts.Filters.Add<ValidationActionFilter>());
        return services;
    }
}

/// <summary>
/// 於 action 執行前，對每個已綁定的 action 參數解析對應的 <see cref="IValidator{T}"/> 並執行驗證；
/// 任一驗證失敗即彙整欄位錯誤並拋出 <see cref="ValidationException"/>（422）。
/// </summary>
public sealed class ValidationActionFilter : IAsyncActionFilter
{
    /// <inheritdoc/>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var failures = new Dictionary<string, string[]>();

        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null) continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (context.HttpContext.RequestServices.GetService(validatorType) is not IValidator validator)
                continue;

            var result = await validator.ValidateAsync(new ValidationContext<object>(argument));
            if (result.IsValid) continue;

            foreach (var error in result.Errors)
            {
                var key = error.PropertyName;
                failures[key] = failures.TryGetValue(key, out var existing)
                    ? [.. existing, error.ErrorMessage]
                    : [error.ErrorMessage];
            }
        }

        if (failures.Count > 0)
            throw new ValidationException("輸入驗證失敗。", failures);

        await next();
    }
}
