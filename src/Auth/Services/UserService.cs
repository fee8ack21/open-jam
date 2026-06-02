using System.Security.Cryptography;
using System.Text.Json;
using Auth.Data;
using Auth.Data.Entities;
using Auth.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shared.Events;

namespace Auth.Services;

public class UserService(AppDbContext db, IOptions<AppOptions> appOptions) : IUserService
{
    public async Task<(bool Success, string? Error)> RegisterAsync(string email, string password)
    {
        if (await db.Users.AnyAsync(u => u.Email == email))
            return (false, "此電子信箱已被使用");

        await using var tx = await db.Database.BeginTransactionAsync();

        var userId    = Guid.NewGuid();
        var token     = GenerateToken();
        var outboxId  = Guid.NewGuid();
        var verifyUrl = $"{appOptions.Value.BaseUrl}/verify-email?token={token}";

        db.Users.Add(new User
        {
            Id           = userId,
            Email        = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            IsVerified   = false,
            CreatedAt    = DateTime.UtcNow
        });

        db.EmailVerificationTokens.Add(new EmailVerificationToken
        {
            Id        = Guid.NewGuid(),
            UserId    = userId,
            Token     = token,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        });

        db.OutboxMessages.Add(new OutboxMessage
        {
            Id        = outboxId,
            EventType = "email.verification",
            Payload   = JsonSerializer.Serialize(new EmailRequestedEvent(
                OutboxMessageId: outboxId,
                To:              email,
                Subject:         "Open Jam 帳號驗證",
                BodyHtml:        $"<p>感謝您註冊 Open Jam！</p><p>請點擊以下連結驗證您的電子信箱：</p><p><a href='{verifyUrl}'>驗證帳號</a></p><p>連結將在 24 小時後失效。</p>",
                EventType:       "email.verification"
            )),
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> SendPasswordResetAsync(string email)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user is null) return (true, null); // 不揭露信箱是否存在

        await using var tx = await db.Database.BeginTransactionAsync();

        var token    = GenerateToken();
        var outboxId = Guid.NewGuid();
        var resetUrl = $"{appOptions.Value.BaseUrl}/reset?token={token}";

        db.PasswordResetTokens.Add(new PasswordResetToken
        {
            Id        = Guid.NewGuid(),
            UserId    = user.Id,
            Token     = token,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        });

        db.OutboxMessages.Add(new OutboxMessage
        {
            Id        = outboxId,
            EventType = "email.password_reset",
            Payload   = JsonSerializer.Serialize(new EmailRequestedEvent(
                OutboxMessageId: outboxId,
                To:              email,
                Subject:         "Open Jam 密碼重置",
                BodyHtml:        $"<p>我們收到了您的密碼重置請求。</p><p>請點擊以下連結重置您的密碼：</p><p><a href='{resetUrl}'>重置密碼</a></p><p>連結將在 1 小時後失效。若非您本人操作，請忽略此信。</p>",
                EventType:       "email.password_reset"
            )),
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return (true, null);
    }

    private static string GenerateToken() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLower();
}
