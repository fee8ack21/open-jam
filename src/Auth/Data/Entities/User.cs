using Shared.Audit;

namespace Auth.Data.Entities;

/// <summary>平台帳號。</summary>
public class User : ICreatedAt, IUpdatedAt
{
    /// <summary>帳號唯一識別碼。</summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>電子信箱（unique）。</summary>
    public string Email { get; set; } = "";

    /// <summary>Argon2id 雜湊後的密碼。</summary>
    public string PasswordHash { get; set; } = "";

    /// <summary>帳號狀態。</summary>
    public UserStatus Status { get; set; } = UserStatus.Pending;

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <inheritdoc/>
    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>關聯的信箱驗證 token 清單。</summary>
    public ICollection<EmailVerificationToken> EmailVerificationTokens { get; set; } = [];

    /// <summary>關聯的密碼重置 token 清單。</summary>
    public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = [];
}
