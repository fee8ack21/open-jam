using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Shared.Auth;

namespace StoreService.Data;

/// <summary>
/// Design-time factory，供 dotnet ef migrations 指令使用。
/// </summary>
public class StoreDbContextFactory : IDesignTimeDbContextFactory<StoreDbContext>
{
    public StoreDbContext CreateDbContext(string[] args)
    {
        var opts = new DbContextOptionsBuilder<StoreDbContext>()
            .UseNpgsql("Host=localhost;Database=open_jam_store;Username=postgres;Password=postgres")
            .UseSnakeCaseNamingConvention()
            .Options;

        return new StoreDbContext(opts, new NullCurrentUserAccessor());
    }
}
