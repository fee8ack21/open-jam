namespace Shared.Auth;

/// <summary>
/// 永遠回傳 null 的 ICurrentUserAccessor 實作。
/// 供 EF Core design-time factory（dotnet ef migrations）等無 HTTP / Worker 情境使用。
/// </summary>
public sealed class NullCurrentUserAccessor : ICurrentUserAccessor
{
    /// <inheritdoc/>
    public Guid? UserId => null;
}
