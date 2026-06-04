using Auth.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Auth;
using Shared.Data;

namespace Auth.Data;

/// <summary>Auth service 的 EF Core DbContext，繼承 BaseDbContext 取得 Audit 自動填入能力。</summary>
public class AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserAccessor currentUser)
    : BaseDbContext(options, currentUser)
{
    /// <summary>帳號資料表。</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>信箱驗證 token 資料表。</summary>
    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();

    /// <summary>密碼重置 token 資料表。</summary>
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    /// <summary>Outbox 訊息資料表。</summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Email).HasMaxLength(255).IsRequired();
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.PasswordHash).IsRequired();
            e.Property(u => u.Status).IsRequired();
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
