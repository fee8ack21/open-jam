using Microsoft.EntityFrameworkCore;
using OrderService.Data.Entities;
using Shared.Auth;
using Shared.Data;

namespace OrderService.Data;

public class OrderDbContext(DbContextOptions<OrderDbContext> options, ICurrentUserAccessor currentUser)
    : BaseDbContext(options, currentUser)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Order>(e =>
        {
            e.HasKey(o => o.Id);
            e.Property(o => o.OrderNumber).HasMaxLength(30).IsRequired();
            e.Property(o => o.BuyerEmail).HasMaxLength(320).IsRequired();
            e.Property(o => o.Currency).HasMaxLength(3).IsRequired();
            e.HasIndex(o => o.OrderNumber).IsUnique();
            e.HasIndex(o => o.StoreId);
            e.HasIndex(o => o.BuyerUserId);
            e.HasIndex(o => o.BuyerEmail);
            e.HasIndex(o => o.Status);

            e.HasMany(o => o.Items)
                .WithOne()
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(o => o.StatusHistory)
                .WithOne()
                .HasForeignKey(h => h.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        model.Entity<OrderItem>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.CatalogName).HasMaxLength(200).IsRequired();
            e.HasIndex(i => i.OrderId);
            e.HasIndex(i => i.CatalogId);
        });

        model.Entity<OrderStatusHistory>(e =>
        {
            e.HasKey(h => h.Id);
            e.HasIndex(h => h.OrderId);
        });

        model.Entity<OutboxMessage>(e =>
        {
            e.HasKey(o => o.Id);
            e.HasIndex(o => o.ProcessedAt);
        });

        base.OnModelCreating(model);
    }
}
