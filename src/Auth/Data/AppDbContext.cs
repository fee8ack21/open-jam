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

    /// <summary>法律文件（服務條款 / 隱私權政策）版本資料表。</summary>
    public DbSet<LegalDocument> LegalDocuments => Set<LegalDocument>();

    /// <summary>使用者法律文件同意紀錄資料表。</summary>
    public DbSet<UserLegalConsent> UserLegalConsents => Set<UserLegalConsent>();

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

        model.Entity<LegalDocument>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.Title).HasMaxLength(200).IsRequired();
            e.Property(d => d.Content).IsRequired();
            e.HasIndex(d => new { d.Type, d.Version }).IsUnique();
            // 同一類型同時僅允許一筆 Active（partial unique index，值對應 LegalDocumentStatus.Active）
            e.HasIndex(d => d.Type).IsUnique().HasFilter("status = 1").HasDatabaseName("ix_legal_documents_type_active");
        });

        model.Entity<UserLegalConsent>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => new { c.UserId, c.LegalDocumentId }).IsUnique();
            e.HasOne(c => c.User)
             .WithMany()
             .HasForeignKey(c => c.UserId);
            e.HasOne(c => c.LegalDocument)
             .WithMany()
             .HasForeignKey(c => c.LegalDocumentId);
        });

        model.Entity<OutboxMessage>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.EventType).HasMaxLength(100).IsRequired();
            e.Property(m => m.Payload).HasColumnType("jsonb").IsRequired();
        });

        base.OnModelCreating(model);
    }
}
