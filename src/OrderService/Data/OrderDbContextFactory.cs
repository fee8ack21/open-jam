using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Shared.Auth;

namespace OrderService.Data;

public class OrderDbContextFactory : IDesignTimeDbContextFactory<OrderDbContext>
{
    public OrderDbContext CreateDbContext(string[] args)
    {
        var opts = new DbContextOptionsBuilder<OrderDbContext>()
            .UseNpgsql("Host=localhost;Database=open_jam_order;Username=postgres;Password=postgres")
            .UseSnakeCaseNamingConvention()
            .Options;

        return new OrderDbContext(opts, new NullCurrentUserAccessor());
    }
}
