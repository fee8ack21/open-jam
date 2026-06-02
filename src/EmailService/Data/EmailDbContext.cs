using EmailService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EmailService.Data;

public class EmailDbContext(DbContextOptions<EmailDbContext> options) : DbContext(options)
{
    public DbSet<EmailRecord> EmailRecords => Set<EmailRecord>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<EmailRecord>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasIndex(r => r.OutboxMessageId).IsUnique();
            e.Property(r => r.To).HasMaxLength(255).IsRequired();
            e.Property(r => r.Subject).HasMaxLength(500).IsRequired();
            e.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
        });
    }
}
