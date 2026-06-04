using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Shared.Auth;

namespace LogService.Data;

/// <summary>
/// Design-time factory，供 dotnet ef migrations 指令使用。
/// </summary>
public class LogDbContextFactory : IDesignTimeDbContextFactory<LogDbContext>
{
    public LogDbContext CreateDbContext(string[] args)
    {
        var opts = new DbContextOptionsBuilder<LogDbContext>()
            .UseNpgsql("Host=localhost;Database=open_jam_log;Username=postgres;Password=postgres")
            .UseSnakeCaseNamingConvention()
            .Options;

        return new LogDbContext(opts, new NullCurrentUserAccessor());
    }

    private sealed class NullCurrentUserAccessor : ICurrentUserAccessor
    {
        public Guid? UserId => null;
    }
}
