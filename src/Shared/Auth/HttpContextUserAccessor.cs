using Microsoft.AspNetCore.Http;

namespace Shared.Auth;

/// <summary>
/// 從 JWT Claims 取得目前登入使用者 ID 的實作。
/// 未登入時 UserId 為 null。
/// </summary>
public sealed class HttpContextUserAccessor(IHttpContextAccessor httpContextAccessor) : ICurrentUserAccessor
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
