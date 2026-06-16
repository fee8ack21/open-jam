using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Shared.Auth;

namespace CatalogService.Data;

/// <summary>Design-time factory，供 dotnet ef migrations 指令使用。</summary>
public class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args)
    {
        var opts = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql("Host=localhost;Database=open_jam_catalog;Username=postgres;Password=postgres")
            .UseSnakeCaseNamingConvention()
            .Options;

        return new CatalogDbContext(opts, new NullCurrentUserAccessor());
    }
}
