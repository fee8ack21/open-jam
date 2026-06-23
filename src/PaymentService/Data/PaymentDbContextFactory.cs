using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Shared.Auth;

namespace PaymentService.Data;

public class PaymentDbContextFactory : IDesignTimeDbContextFactory<PaymentDbContext>
{
    public PaymentDbContext CreateDbContext(string[] args)
    {
        var opts = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseNpgsql("Host=localhost;Database=open_jam_payment;Username=postgres;Password=postgres")
            .UseSnakeCaseNamingConvention()
            .Options;

        return new PaymentDbContext(opts, new NullCurrentUserAccessor());
    }
}
