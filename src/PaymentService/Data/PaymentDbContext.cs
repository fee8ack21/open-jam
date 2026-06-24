using Microsoft.EntityFrameworkCore;
using PaymentService.Data.Entities;
using Shared.Auth;
using Shared.Data;

namespace PaymentService.Data;

public class PaymentDbContext(DbContextOptions<PaymentDbContext> options, ICurrentUserAccessor currentUser)
    : BaseDbContext(options, currentUser)
{
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<ProviderEvent> ProviderEvents => Set<ProviderEvent>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Payment>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Provider).HasMaxLength(20).IsRequired();
            e.Property(p => p.Currency).HasMaxLength(3).IsRequired();
            e.Property(p => p.Email).HasMaxLength(320).IsRequired();
            e.Property(p => p.ProviderPaymentId).HasMaxLength(100);
            e.Property(p => p.ProviderCheckoutId).HasMaxLength(100);
            e.HasIndex(p => p.OrderId, "ix_payments_order_id");
            e.HasIndex(p => p.ProviderCheckoutId);
            e.HasIndex(p => p.ProviderPaymentId);
            e.HasIndex(p => p.Status);

            // 同一訂單只允許一筆未完成（Pending）付款，避免重複建立 Checkout Session（race condition 下的最後防線）。
            e.HasIndex(p => p.OrderId, "ix_payments_order_id_pending")
                .HasDatabaseName("ix_payments_order_id_pending")
                .IsUnique()
                .HasFilter("status = 0");
        });

        model.Entity<PaymentTransaction>(e =>
        {
            e.HasKey(t => t.Id);
            e.HasIndex(t => t.PaymentId);
            e.HasIndex(t => t.ProviderTransactionId);
        });

        model.Entity<ProviderEvent>(e =>
        {
            e.HasKey(ev => ev.Id);
            e.HasIndex(ev => new { ev.Provider, ev.EventId }).IsUnique();
            e.HasIndex(ev => ev.ProcessedAt);
            e.Property(ev => ev.EventId).HasMaxLength(100).IsRequired();
            e.Property(ev => ev.EventType).HasMaxLength(100).IsRequired();
            e.Property(ev => ev.RawPayload).IsRequired();
        });

        model.Entity<OutboxMessage>(e =>
        {
            e.HasKey(o => o.Id);
            e.HasIndex(o => o.ProcessedAt);
        });

        base.OnModelCreating(model);
    }
}
