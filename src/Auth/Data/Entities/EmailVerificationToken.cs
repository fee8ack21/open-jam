using Shared.Audit;

namespace Auth.Data.Entities;

/// <summary>信箱驗證 token；每次註冊或重發驗證信時建立一筆。</summary>
public class EmailVerificationToken : ICreatedAt
{
    /// <summary>Token 唯一識別碼。</summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>關聯的帳號 ID。</summary>
    public Guid UserId { get; set; }

    /// <summary>驗證用的隨機 hex 字串（64 字元）。</summary>
    public string Token { get; set; } = "";

    /// <summary>Token 到期時間（UTC）。</summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>Token 使用時間；null 表示尚未使用。</summary>
    public DateTimeOffset? UsedAt { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>關聯的帳號。</summary>
    public User User { get; set; } = null!;
}
