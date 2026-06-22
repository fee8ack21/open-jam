using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Shared.Auth;

namespace QuotaService.Data;

/// <summary>Design-time factory，供 dotnet ef migrations 指令使用。</summary>
public class QuotaDbContextFactory : IDesignTimeDbContextFactory<QuotaDbContext>
{
    public QuotaDbContext CreateDbContext(string[] args)
    {
        var opts = new DbContextOptionsBuilder<QuotaDbContext>()
            .UseNpgsql("Host=localhost;Database=open_jam_quota;Username=postgres;Password=postgres")
            .UseSnakeCaseNamingConvention()
            .Options;

        return new QuotaDbContext(opts, new NullCurrentUserAccessor());
    }
}
