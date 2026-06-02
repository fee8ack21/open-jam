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
                TemplateKey:     "email.verification",
                Params:          new() { ["VerifyUrl"] = verifyUrl, ["ExpiresInHours"] = "24" },
                Locale:          "zh-TW",
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
                TemplateKey:     "email.password_reset",
                Params:          new() { ["ResetUrl"] = resetUrl, ["ExpiresInHours"] = "1" },
                Locale:          "zh-TW",
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
