using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Shared.Auth;

namespace NotificationService.Data;

public class NotificationDbContextFactory : IDesignTimeDbContextFactory<NotificationDbContext>
{
    public NotificationDbContext CreateDbContext(string[] args)
    {
        var opts = new DbContextOptionsBuilder<NotificationDbContext>()
            .UseNpgsql("Host=localhost;Database=open_jam_notification;Username=postgres;Password=postgres")
            .UseSnakeCaseNamingConvention()
            .Options;

        return new NotificationDbContext(opts, new NullCurrentUserAccessor());
    }
}
