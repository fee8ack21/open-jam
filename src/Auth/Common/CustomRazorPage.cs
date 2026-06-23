using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Localization;

namespace Auth.Common;

public abstract class CustomRazorPage<TModel> : RazorPage<TModel>
{
    private IStringLocalizer? _localizer;

    public IStringLocalizer T
    {
        get
        {
            if (_localizer == null)
            {
                var factory = Context.RequestServices.GetRequiredService<IStringLocalizerFactory>();
                var path = ViewContext.ExecutingFilePath;
                var baseName = "Auth.Resources" + path?.Replace("/Views", "").Replace(".cshtml", "").Replace('/', '.');
                _localizer = factory.Create(baseName!, "Auth");
            }
            return _localizer;
        }
    }
}
