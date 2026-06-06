using Shared.Auth;

namespace EmailService.Services.Auth;

/// <summary>
/// Worker 服務用的 ICurrentUserAccessor 實作，永遠回傳 null。
/// EmailService 為背景工作，所有操作皆以系統身份執行，無 HTTP context。
/// </summary>
public sealed class WorkerCurrentUserAccessor : ICurrentUserAccessor
{
    /// <inheritdoc/>
    public Guid? UserId => null;
}
