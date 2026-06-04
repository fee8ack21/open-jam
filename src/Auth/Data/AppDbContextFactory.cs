using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Shared.Auth;

namespace Auth.Data;

/// <summary>EF Core design-time factory，供 dotnet ef migrations 使用。</summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    /// <inheritdoc/>
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=open_jam_auth;Username=postgres;Password=postgres")
            .UseSnakeCaseNamingConvention()
            .Options;
        return new AppDbContext(options, new NullCurrentUserAccessor());
    }

    private sealed class NullCurrentUserAccessor : ICurrentUserAccessor
    {
        public Guid? UserId => null;
    }
}
