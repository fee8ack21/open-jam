using EmailService.Data;
using EmailService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bootstrap.Seeders;

public class EmailTemplateSeeder(AppDbContext db, ILogger<EmailTemplateSeeder> logger)
{
    private static readonly string ResourcesDir =
        Path.Combine(AppContext.BaseDirectory, "Resources", "email-templates");

    public async Task SeedAsync()
    {
        await db.Database.MigrateAsync();
        logger.LogInformation("Email DB migrations applied");

        var activationHtml = await File.ReadAllTextAsync(
            Path.Combine(ResourcesDir, "account-activation-email-template.html"));

        var passwordResetHtml = await File.ReadAllTextAsync(
            Path.Combine(ResourcesDir, "password-reset-email-template.html"));

        await UpsertAsync(
            "email.verification",
            "帳號開通驗證信",
            [("zh-TW", "Open Jam · 開通你的帳號", activationHtml)]);

        await UpsertAsync(
            "email.password_reset",
            "密碼重置信",
            [("zh-TW", "Open Jam · 重置你的密碼", passwordResetHtml)]);

        var passwordChangedHtml = await File.ReadAllTextAsync(
            Path.Combine(ResourcesDir, "password-changed-email-template.html"));

        var emailChangeConfirmHtml = await File.ReadAllTextAsync(
            Path.Combine(ResourcesDir, "email-change-confirm-email-template.html"));

        var emailChangeNotifyHtml = await File.ReadAllTextAsync(
            Path.Combine(ResourcesDir, "email-change-notify-email-template.html"));

        var newDeviceLoginHtml = await File.ReadAllTextAsync(
            Path.Combine(ResourcesDir, "new-device-login-email-template.html"));

        var accountLockedHtml = await File.ReadAllTextAsync(
            Path.Combine(ResourcesDir, "account-locked-email-template.html"));

        await UpsertAsync(
            "email.password_changed",
            "密碼已變更通知",
            [("zh-TW", "Open Jam · 密碼已成功更新", passwordChangedHtml)]);

        await UpsertAsync(
            "email.email_change_confirm",
            "信箱變更確認（新信箱）",
            [("zh-TW", "Open Jam · 確認你的新信箱", emailChangeConfirmHtml)]);

        await UpsertAsync(
            "email.email_change_notify",
            "信箱變更通知（舊信箱）",
            [("zh-TW", "Open Jam · 信箱已成功更換", emailChangeNotifyHtml)]);

        await UpsertAsync(
            "email.new_device_login",
            "新裝置 / 異常登入提醒",
            [("zh-TW", "Open Jam · 新裝置登入提醒", newDeviceLoginHtml)]);

        await UpsertAsync(
            "email.account_locked",
            "帳號鎖定 / 停權通知",
            [("zh-TW", "Open Jam · 帳號已暫時鎖定", accountLockedHtml)]);

        var storeApplicationApprovedHtml = await File.ReadAllTextAsync(
            Path.Combine(ResourcesDir, "store-application-approved-email-template.html"));

        var storeApplicationRejectedHtml = await File.ReadAllTextAsync(
            Path.Combine(ResourcesDir, "store-application-rejected-email-template.html"));

        await UpsertAsync(
            "email.store_application_approved",
            "開店申請已核准通知",
            [("zh-TW", "Open Jam · 你的商店申請已核准", storeApplicationApprovedHtml)]);

        await UpsertAsync(
            "email.store_application_rejected",
            "開店申請已駁回通知",
            [("zh-TW", "Open Jam · 你的商店申請審核結果", storeApplicationRejectedHtml)]);

        await db.SaveChangesAsync();
        logger.LogInformation("Email templates seeded");
    }

    private async Task UpsertAsync(
        string templateKey,
        string description,
        IEnumerable<(string Locale, string Subject, string BodyHtml)> translations)
    {
        var config = await db.EmailTemplates
            .Include(c => c.Translations)
            .FirstOrDefaultAsync(c => c.Key == templateKey);

        if (config is null)
        {
            config = new EmailTemplate
            {
                Key = templateKey,
                Description = description,
            };
            db.EmailTemplates.Add(config);
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
                config.Translations.Add(new EmailTemplateTranslation
                {
                    Locale    = locale,
                    Subject   = subject,
                    BodyHtml  = bodyHtml,
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
