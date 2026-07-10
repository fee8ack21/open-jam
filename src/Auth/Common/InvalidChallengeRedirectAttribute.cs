using Auth.Services.Hydra;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Auth.Common;

/// <summary>將 InvalidChallengeException 轉為導向錯誤頁，避免非法 challenge 讓使用者看到 500。</summary>
public class InvalidChallengeRedirectAttribute : ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context)
    {
        if (context.Exception is not InvalidChallengeException) return;

        context.Result = new RedirectToActionResult("Error", "Home", routeValues: null);
        context.ExceptionHandled = true;
    }
}
