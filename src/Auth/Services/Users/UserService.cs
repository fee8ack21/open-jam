using System.Security.Cryptography;
using System.Text.Json;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Auth.Data;
using Auth.Data.Entities;
using Auth.Models;
using Auth.Options;
using Auth.Services.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shared.Data;
using Shared.Events;

namespace Auth.Services.Users;

/// <summary>帳號相關業務邏輯的具體實作。</summary>
public class UserService(
    AppDbContext db,
    IPasswordHasher passwordHasher,
    IMapper mapper,
    IOptions<AppOptions> appOptions) : IUserService
{
    /// <inheritdoc/>
    public async Task<ListUsersResponse> ListAsync(ListUsersRequest request, CancellationToken ct = default)
    {
        var query = db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var keyword = request.Search.Trim().ToLower();
            query = query.Where(u => u.Email.ToLower().Contains(keyword));
        }

        if (request.Role.HasValue)
            query = query.Where(u => u.Role == request.Role);

        if (request.Status.HasValue)
            query = query.Where(u => u.Status == request.Status);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip(request.Offset)
            .Take(request.Limit)
            .ProjectTo<UserSummaryDto>(mapper.ConfigurationProvider)
            .ToListAsync(ct);

        return new ListUsersResponse { TotalCount = total, Items = items };
    }

    /// <inheritdoc/>
    public async Task<(bool Success, string? Error)> RegisterAsync(string email, string password)
    {
        email = NormalizeEmail(email);
        var existing = await db.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (existing is not null && existing.Status != UserStatus.Pending)
            return (false, "此電子信箱已被使用");

        // 啟用 EnableRetryOnFailure 後，顯式交易須包進 execution strategy 才能在 transient 失敗時整體重放。
        return await db.Database.ExecuteInTransactionAsync<(bool, string?)>(async tx =>
        {
            var hash = passwordHasher.Hash(password);
            User user;

            if (existing is not null)
            {
                // Squatting 防護：同一信箱已有 Pending 帳號 → 覆蓋並重發驗證信
                existing.PasswordHash = hash;

                // 作廢舊的未使用 token
                await db.EmailVerificationTokens
                    .Where(t => t.UserId == existing.Id && t.UsedAt == null)
                    .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsedAt, DateTimeOffset.UtcNow));

                user = existing;
            }
            else
            {
                user = new User { Email = email, PasswordHash = hash, Status = UserStatus.Pending };
                db.Users.Add(user);
            }

            var (token, outbox) = BuildVerificationOutbox(user.Id, email);
            db.EmailVerificationTokens.Add(token);
            db.OutboxMessages.Add(outbox);

            await db.SaveChangesAsync();
            await tx.CommitAsync();

            return (true, null);
        });
    }

    /// <inheritdoc/>
    public async Task<(bool Success, string? Error)> VerifyEmailAsync(string token)
    {
        var record = await db.EmailVerificationTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == token);

        if (record is null)
            return (false, "驗證連結無效");

        if (record.UsedAt.HasValue)
            return (false, "此驗證連結已使用過");

        if (record.ExpiresAt < DateTimeOffset.UtcNow)
            return (false, "驗證連結已過期，請重新註冊或申請補發");

        record.UsedAt      = DateTimeOffset.UtcNow;
        record.User.Status = UserStatus.Active;

        await db.SaveChangesAsync();
        return (true, null);
    }

    /// <inheritdoc/>
    public async Task<(bool Success, string? Subject, string? Error)> LoginAsync(string email, string password)
    {
        email = NormalizeEmail(email);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user is null || !passwordHasher.Verify(password, user.PasswordHash))
            return (false, null, "電子信箱或密碼錯誤");

        var error = user.Status switch
        {
            UserStatus.Pending     => "帳號尚未驗證，請至信箱點擊開通連結",
            UserStatus.Locked      => "帳號因多次登入失敗已暫時鎖定，請稍後再試",
            UserStatus.Suspended   => "帳號已停權，請聯繫客服",
            UserStatus.Deactivated => "帳號已停用",
            UserStatus.Deleted     => "電子信箱或密碼錯誤",
            _                      => null,
        };

        if (error is not null)
            return (false, null, error);

        return (true, user.Id.ToString(), null);
    }

    /// <inheritdoc/>
    public async Task<(bool Success, string? Error)> SendPasswordResetAsync(string email)
    {
        email = NormalizeEmail(email);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);

        // 無論信箱是否存在一律成功，防止帳號列舉
        if (user is null || user.Status is UserStatus.Deleted)
            return (true, null);

        // 啟用 EnableRetryOnFailure 後，顯式交易須包進 execution strategy 才能在 transient 失敗時整體重放。
        return await db.Database.ExecuteInTransactionAsync<(bool, string?)>(async tx =>
        {
            // 作廢舊的未使用重置 token
            await db.PasswordResetTokens
                .Where(t => t.UserId == user.Id && t.UsedAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsedAt, DateTimeOffset.UtcNow));

            var (resetToken, outbox) = BuildPasswordResetOutbox(user.Id, email);
            db.PasswordResetTokens.Add(resetToken);
            db.OutboxMessages.Add(outbox);

            await db.SaveChangesAsync();
            await tx.CommitAsync();

            return (true, null);
        });
    }

    /// <inheritdoc/>
    public async Task<(bool Success, string? Error)> ResetPasswordAsync(string token, string newPassword)
    {
        var record = await db.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == token);

        if (record is null)
            return (false, "重置連結無效");

        if (record.UsedAt.HasValue)
            return (false, "此重置連結已使用過");

        if (record.ExpiresAt < DateTimeOffset.UtcNow)
            return (false, "重置連結已過期，請重新申請");

        record.UsedAt            = DateTimeOffset.UtcNow;
        record.User.PasswordHash = passwordHasher.Hash(newPassword);

        await db.SaveChangesAsync();
        return (true, null);
    }

    // ── 私有輔助方法 ───────────────────────────────────────────────────────────

    private (EmailVerificationToken Token, OutboxMessage Outbox) BuildVerificationOutbox(Guid userId, string email)
    {
        var tokenStr  = GenerateToken();
        var baseUrl   = appOptions.Value.BaseUrl.TrimEnd('/');
        var verifyUrl = $"{baseUrl}/verify-email?token={tokenStr}";
        var expiresIn = TimeSpan.FromHours(24);

        var token = new EmailVerificationToken
        {
            UserId    = userId,
            Token     = tokenStr,
            ExpiresAt = DateTimeOffset.UtcNow.Add(expiresIn),
        };

        var outbox = new OutboxMessage { EventType = "email.verification" };
        outbox.Payload = JsonSerializer.Serialize(new EmailRequestedEvent(
            OutboxMessageId: outbox.Id,
            To:              email,
            TemplateKey:     "email.verification",
            Params:          new() { ["activation_url"] = verifyUrl, ["expires_in_hours"] = expiresIn.TotalHours.ToString("0") },
            Locale:          "zh-TW"
        ));

        return (token, outbox);
    }

    private (PasswordResetToken Token, OutboxMessage Outbox) BuildPasswordResetOutbox(Guid userId, string email)
    {
        var tokenStr = GenerateToken();
        var baseUrl  = appOptions.Value.BaseUrl.TrimEnd('/');
        var resetUrl = $"{baseUrl}/reset?token={tokenStr}";
        var expiresIn = TimeSpan.FromMinutes(30);

        var token = new PasswordResetToken
        {
            UserId    = userId,
            Token     = tokenStr,
            ExpiresAt = DateTimeOffset.UtcNow.Add(expiresIn),
        };

        var outbox = new OutboxMessage { EventType = "email.password_reset" };
        outbox.Payload = JsonSerializer.Serialize(new EmailRequestedEvent(
            OutboxMessageId: outbox.Id,
            To:              email,
            TemplateKey:     "email.password_reset",
            Params:          new() { ["reset_url"] = resetUrl, ["expires_in_minutes"] = expiresIn.TotalMinutes.ToString("0") },
            Locale:          "zh-TW"
        ));

        return (token, outbox);
    }

    private static string GenerateToken() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLower();

    /// <summary>Email 一律以 trim + 小寫儲存與查詢，避免 Postgres 大小寫敏感造成同信箱重複註冊或登入失敗。</summary>
    private static string NormalizeEmail(string email) =>
        email.Trim().ToLowerInvariant();

    /// <inheritdoc/>
    public async Task<Dictionary<string, object>?> GetAccessTokenClaimsAsync(string subject)
    {
        if (!Guid.TryParse(subject, out var userId))
            return null;

        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return null;

        return new Dictionary<string, object>
        {
            ["role"] = user.Role.ToString(),
            ["email"] = user.Email,
        };
    }
}
