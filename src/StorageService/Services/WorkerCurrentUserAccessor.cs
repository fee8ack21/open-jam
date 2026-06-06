using Shared.Auth;

namespace StorageService.Services;

/// <summary>HTTP 請求情境中從 JWT Claims 讀取 UserId 的實作。</summary>
public class HttpContextUserAccessor(IHttpContextAccessor httpContextAccessor) : ICurrentUserAccessor
{
    /// <inheritdoc/>
    public Guid? UserId
    {
        get
        {
            var sub = httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }
}
