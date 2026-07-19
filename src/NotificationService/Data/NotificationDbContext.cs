using Microsoft.EntityFrameworkCore;
using NotificationService.Data.Entities;
using Shared.Auth;
using Shared.Data;

namespace NotificationService.Data;

public class NotificationDbContext(DbContextOptions<NotificationDbContext> options, ICurrentUserAccessor currentUser)
    : BaseDbContext(options, currentUser)
{
    public DbSet<NotificationRequest> NotificationRequests => Set<NotificationRequest>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<StoreFollowerRef> StoreFollowerRefs => Set<StoreFollowerRef>();
    public DbSet<CatalogBuyerRef> CatalogBuyerRefs => Set<CatalogBuyerRef>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<NotificationRequest>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Type).HasMaxLength(100).IsRequired();
            e.Property(r => r.Payload).HasColumnType("jsonb").IsRequired();
            e.Property(r => r.LastError).HasMaxLength(2000);
            e.HasIndex(r => new { r.Status, r.ScheduledAt });
            e.HasIndex(r => r.StoreId);
            e.HasIndex(r => r.SourceEventId).IsUnique();
        });

        model.Entity<Notification>(e =>
        {
            e.HasKey(n => n.Id);
            e.Property(n => n.RecipientEmail).HasMaxLength(320).IsRequired();
            e.Property(n => n.Type).HasMaxLength(100).IsRequired();
            e.Property(n => n.Payload).HasColumnType("jsonb").IsRequired();
            e.HasIndex(n => new { n.RecipientUserId, n.ReadAt });
            e.HasIndex(n => new { n.RequestId, n.RecipientEmail }).IsUnique();
        });

        model.Entity<StoreFollowerRef>(e =>
        {
            e.HasKey(f => f.Id);
            e.Property(f => f.Email).HasMaxLength(320).IsRequired();
            e.HasIndex(f => new { f.StoreId, f.Email }).IsUnique();
            e.HasIndex(f => f.Email);
        });

        model.Entity<CatalogBuyerRef>(e =>
        {
            e.HasKey(b => b.Id);
            e.Property(b => b.Email).HasMaxLength(320).IsRequired();
            e.HasIndex(b => new { b.CatalogId, b.Email }).IsUnique();
            e.HasIndex(b => b.Email);
        });

        model.Entity<OutboxMessage>(e =>
        {
            e.HasKey(o => o.Id);
            e.HasIndex(o => o.ProcessedAt);
        });

        model.Entity<ProcessedEvent>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => p.OutboxMessageId).IsUnique();
        });

        base.OnModelCreating(model);
    }
}
