using System.Security.Cryptography;
using System.Text.Json;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Auth.Data;
using Auth.Data.Entities;
using Auth.Models;
using Auth.Options;
using Auth.Services.Legal;
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
    ILegalConsentService legalConsentService,
    IOptions<AppOptions> appOptions,
    IOptions<SecurityOptions> securityOptions) : IUserService
{
    private const string InvalidCredentialsError = "電子信箱或密碼錯誤";
    private const string LockedError = "帳號因多次登入失敗已暫時鎖定，請稍後再試";

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

        // 重發驗證信節流（mail-bomb 防護）：同一 Pending 帳號覆蓋重註冊即重寄驗證信，須有冷卻與次數上限
        if (existing is not null &&
            await GetEmailThrottleErrorAsync(
                db.EmailVerificationTokens.Where(t => t.UserId == existing.Id).Select(t => t.CreatedAt), "驗證信") is { } throttleError)
            return (false, throttleError);

        // 同意紀錄依據：註冊當下啟用中的條款版本，向 ContentService 即時取得（交易外，
        // 服務不可用時擲出例外中止註冊，不建立帳號）。
        var activeDocIds = (await legalConsentService.GetActiveAsync()).Select(d => d.Id).ToList();

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

            // 同意紀錄：註冊當下啟用中的條款版本（Pending 帳號覆蓋重註冊時略過已同意者）
            var consentedDocIds = await db.UserLegalConsents
                .Where(c => c.UserId == user.Id && activeDocIds.Contains(c.LegalDocumentId))
                .Select(c => c.LegalDocumentId)
                .ToListAsync();

            foreach (var docId in activeDocIds.Except(consentedDocIds))
                db.UserLegalConsents.Add(new UserLegalConsent { UserId = user.Id, LegalDocumentId = docId });

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

        // 信箱所有權已證明，發布註冊完成事件（StoreService / NotificationService 據此回填追蹤紀錄的 UserId）
        db.OutboxMessages.Add(BuildUserRegisteredOutbox(record.User.Id, record.User.Email));

        await db.SaveChangesAsync();
        return (true, null);
    }

    /// <inheritdoc/>
    public async Task<(bool Success, string? Subject, string? Error)> LoginAsync(string email, string password)
    {
        email = NormalizeEmail(email);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);

        // 帳號不存在（或已刪除）不做失敗計數，暴力嘗試由 IP rate limit 擋
        if (user is null || user.Status is UserStatus.Deleted)
            return (false, null, InvalidCredentialsError);

        var opts = securityOptions.Value;
        var now  = DateTimeOffset.UtcNow;

        // 鎖定期間一律拒絕，不驗密碼也不再累計
        if (user.LockedUntil is { } lockedUntil && lockedUntil > now)
            return (false, null, LockedError);

        // 鎖定期滿：自動解鎖並歸零重新計數
        if (user.LockedUntil is not null)
        {
            user.LockedUntil      = null;
            user.FailedLoginCount = 0;
        }

        if (!passwordHasher.Verify(password, user.PasswordHash))
        {
            user.FailedLoginCount++;

            if (user.FailedLoginCount >= opts.MaxFailedLoginAttempts)
            {
                user.LockedUntil = now.AddMinutes(opts.LockoutMinutes);
                db.OutboxMessages.Add(BuildAccountLockedOutbox(user.Email, user.LockedUntil.Value));
                await db.SaveChangesAsync();
                return (false, null, LockedError);
            }

            await db.SaveChangesAsync();
            return (false, null, InvalidCredentialsError);
        }

        var error = user.Status switch
        {
            UserStatus.Pending     => "帳號尚未驗證，請至信箱點擊開通連結",
            UserStatus.Locked      => LockedError,
            UserStatus.Suspended   => "帳號已停權，請聯繫客服",
            UserStatus.Deactivated => "帳號已停用",
            _                      => null,
        };

        if (error is not null)
            return (false, null, error);

        // 成功登入：清除失敗計數
        if (user.FailedLoginCount > 0 || user.LockedUntil is not null)
        {
            user.FailedLoginCount = 0;
            user.LockedUntil      = null;
            await db.SaveChangesAsync();
        }

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

        // 重置信節流（mail-bomb 防護）：冷卻時間內或超過每小時上限時靜默略過寄信，
        // 對外仍回成功（與防列舉的統一訊息一致，不洩漏節流狀態）
        if (await GetEmailThrottleErrorAsync(
                db.PasswordResetTokens.Where(t => t.UserId == user.Id).Select(t => t.CreatedAt), "重置信") is not null)
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

    /// <summary>
    /// 寄信節流檢查（mail-bomb 防護）：冷卻時間內剛寄過、或一小時內達次數上限時回錯誤訊息；可寄回 null。
    /// </summary>
    /// <param name="sentAts">該帳號同類信件的歷史觸發時間（token CreatedAt）查詢。</param>
    /// <param name="mailName">信件名稱（組錯誤訊息用），如「驗證信」。</param>
    private async Task<string?> GetEmailThrottleErrorAsync(IQueryable<DateTimeOffset> sentAts, string mailName)
    {
        var opts = securityOptions.Value;
        var now  = DateTimeOffset.UtcNow;

        var latest = await sentAts.OrderByDescending(t => t).FirstOrDefaultAsync();
        if (latest > now.AddSeconds(-opts.EmailCooldownSeconds))
            return $"{mailName}剛寄出，請稍候 {opts.EmailCooldownSeconds} 秒後再試";

        var hourCount = await sentAts.CountAsync(t => t > now.AddHours(-1));
        if (hourCount >= opts.MaxEmailsPerHour)
            return $"{mailName}寄送次數過多，請一小時後再試";

        return null;
    }

    private OutboxMessage BuildAccountLockedOutbox(string email, DateTimeOffset lockedUntil)
    {
        // 信件受眾為台灣使用者，解鎖時間以台北時間（UTC+8）呈現
        var unlockAt = lockedUntil.ToOffset(TimeSpan.FromHours(8)).ToString("yyyy/MM/dd HH:mm");

        var outbox = new OutboxMessage { EventType = "email.account_locked" };
        outbox.Payload = JsonSerializer.Serialize(new EmailRequestedEvent(
            OutboxMessageId: outbox.Id,
            To:              email,
            TemplateKey:     "email.account_locked",
            Params:          new() { ["unlock_at"] = $"{unlockAt}（台北時間）" },
            Locale:          "zh-TW"
        ));
        return outbox;
    }

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

    private static OutboxMessage BuildUserRegisteredOutbox(Guid userId, string email)
    {
        var outbox = new OutboxMessage { EventType = "user.registered" };
        outbox.Payload = JsonSerializer.Serialize(new UserRegisteredEvent(
            OutboxMessageId: outbox.Id,
            UserId:          userId,
            Email:           NormalizeEmail(email),
            RegisteredAt:    DateTimeOffset.UtcNow
        ));
        return outbox;
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
