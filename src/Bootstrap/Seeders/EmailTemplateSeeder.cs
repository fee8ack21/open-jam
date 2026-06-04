using EmailService.Data;
using EmailService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bootstrap.Seeders;

public class EmailTemplateSeeder(EmailDbContext db, ILogger<EmailTemplateSeeder> logger)
{
    public async Task SeedAsync()
    {
        await db.Database.MigrateAsync();
        logger.LogInformation("Email DB migrations applied");

        await UpsertAsync(
            "email.verification",
            "帳號開通驗證信",
            [
                ("zh-TW", "Open Jam 帳號驗證", """
                    <p>感謝您註冊 Open Jam！</p>
                    <p>請點擊以下連結驗證您的電子信箱：</p>
                    <p><a href="{{VerifyUrl}}" style="display:inline-block;padding:10px 20px;background:#6d28d9;color:#fff;text-decoration:none;border-radius:4px;">驗證帳號</a></p>
                    <p style="color:#6b7280;font-size:14px;">連結將在 {{ExpiresInHours}} 小時後失效。若非您本人操作，請忽略此信。</p>
                    """)
            ]);

        await UpsertAsync(
            "email.password_reset",
            "密碼重置信",
            [
                ("zh-TW", "Open Jam 密碼重置", """
                    <p>我們收到了您的密碼重置請求。</p>
                    <p>請點擊以下連結重置您的密碼：</p>
                    <p><a href="{{ResetUrl}}" style="display:inline-block;padding:10px 20px;background:#6d28d9;color:#fff;text-decoration:none;border-radius:4px;">重置密碼</a></p>
                    <p style="color:#6b7280;font-size:14px;">連結將在 {{ExpiresInHours}} 小時後失效。若非您本人操作，請忽略此信。</p>
                    """)
            ]);

        await UpsertAsync(
            "email.password_changed",
            "密碼已變更通知",
            [
                ("zh-TW", "Open Jam 密碼已變更", """
                    <p>您的 Open Jam 帳號密碼已成功變更。</p>
                    <p>若非您本人操作，請立即聯絡客服並重置密碼。</p>
                    <p style="color:#6b7280;font-size:14px;">變更時間：{{ChangedAt}}</p>
                    """)
            ]);

        await UpsertAsync(
            "email.email_change_confirm",
            "信箱變更確認（新信箱）",
            [
                ("zh-TW", "Open Jam 新信箱驗證", """
                    <p>您已申請將 Open Jam 帳號信箱變更至此地址。</p>
                    <p>請點擊以下連結確認新信箱：</p>
                    <p><a href="{{ConfirmUrl}}" style="display:inline-block;padding:10px 20px;background:#6d28d9;color:#fff;text-decoration:none;border-radius:4px;">確認新信箱</a></p>
                    <p style="color:#6b7280;font-size:14px;">連結將在 {{ExpiresInHours}} 小時後失效。若非您本人操作，請忽略此信。</p>
                    """)
            ]);

        await UpsertAsync(
            "email.email_change_notify",
            "信箱變更通知（舊信箱）",
            [
                ("zh-TW", "Open Jam 信箱已變更通知", """
                    <p>您的 Open Jam 帳號信箱已變更為新地址。</p>
                    <p>若非您本人操作，請立即聯絡客服。</p>
                    <p style="color:#6b7280;font-size:14px;">變更時間：{{ChangedAt}}</p>
                    """)
            ]);

        await UpsertAsync(
            "email.new_device_login",
            "新裝置 / 異常登入提醒",
            [
                ("zh-TW", "Open Jam 新裝置登入提醒", """
                    <p>我們偵測到您的帳號於新裝置或異常地點登入。</p>
                    <p style="color:#6b7280;font-size:14px;">登入時間：{{LoginAt}}<br>裝置：{{DeviceInfo}}<br>IP：{{IpAddress}}</p>
                    <p>若非您本人操作，請立即修改密碼並聯絡客服。</p>
                    """)
            ]);

        await UpsertAsync(
            "email.account_locked",
            "帳號鎖定 / 停權通知",
            [
                ("zh-TW", "Open Jam 帳號鎖定通知", """
                    <p>您的 Open Jam 帳號因多次登入失敗已暫時鎖定。</p>
                    <p>請於 {{UnlockAt}} 後重試，或聯絡客服解除鎖定。</p>
                    <p style="color:#6b7280;font-size:14px;">若非您本人操作，您的密碼可能已外洩，建議立即重置密碼。</p>
                    """)
            ]);

        await db.SaveChangesAsync();
        logger.LogInformation("Email templates seeded");
    }

    private async Task UpsertAsync(
        string templateKey,
        string description,
        IEnumerable<(string Locale, string Subject, string BodyHtml)> translations)
    {
        var config = await db.EmailConfigs
            .Include(c => c.Translations)
            .FirstOrDefaultAsync(c => c.TemplateKey == templateKey);

        if (config is null)
        {
            config = new EmailConfig
            {
                TemplateKey = templateKey,
                Description = description,
                CreatedAt   = DateTime.UtcNow,
            };
            db.EmailConfigs.Add(config);
            logger.LogInformation("Adding email template '{Key}'", templateKey);
        }
        else
        {
            config.Description = description;
        }

        foreach (var (locale, subject, bodyHtml) in translations)
        {
            var tr = config.Translations.FirstOrDefault(t => t.Locale == locale);
            if (tr is null)
            {
                config.Translations.Add(new EmailConfigTranslation
                {
                    Locale    = locale,
                    Subject   = subject,
                    BodyHtml  = bodyHtml,
                    CreatedAt = DateTime.UtcNow,
                });
            }
            else
            {
                tr.Subject  = subject;
                tr.BodyHtml = bodyHtml;
            }
        }
    }
}
