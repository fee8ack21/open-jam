using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Shared.Auth;

namespace StorageService.Data;

/// <summary>Design-time factory，供 dotnet ef migrations 指令使用。</summary>
public class StorageDbContextFactory : IDesignTimeDbContextFactory<StorageDbContext>
{
    public StorageDbContext CreateDbContext(string[] args)
    {
        var opts = new DbContextOptionsBuilder<StorageDbContext>()
            .UseNpgsql("Host=localhost;Database=open_jam_storage;Username=postgres;Password=postgres")
            .UseSnakeCaseNamingConvention()
            .Options;

        return new StorageDbContext(opts, new NullCurrentUserAccessor());
    }

    private sealed class NullCurrentUserAccessor : ICurrentUserAccessor
    {
        public Guid? UserId => null;
    }
}
