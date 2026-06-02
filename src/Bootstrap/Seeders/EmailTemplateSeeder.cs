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
