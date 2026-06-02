using Auth.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Auth.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User>                   Users                   => Set<User>();
    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();
    public DbSet<PasswordResetToken>     PasswordResetTokens     => Set<PasswordResetToken>();
    public DbSet<OutboxMessage>          OutboxMessages          => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Email).HasMaxLength(255).IsRequired();
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.PasswordHash).IsRequired();
        });

        model.Entity<EmailVerificationToken>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Token).HasMaxLength(64).IsRequired();
            e.HasOne(t => t.User)
             .WithMany(u => u.EmailVerificationTokens)
             .HasForeignKey(t => t.UserId);
        });

        model.Entity<PasswordResetToken>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Token).HasMaxLength(64).IsRequired();
            e.HasOne(t => t.User)
             .WithMany(u => u.PasswordResetTokens)
             .HasForeignKey(t => t.UserId);
        });

        model.Entity<OutboxMessage>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.EventType).HasMaxLength(100).IsRequired();
            e.Property(m => m.Payload).HasColumnType("jsonb").IsRequired();
        });
    }
}
