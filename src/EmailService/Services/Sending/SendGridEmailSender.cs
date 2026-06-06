using EmailService.Options;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace EmailService.Services.Sending;

/// <summary>以 SendGrid API 寄信的 IEmailSender 實作（正式環境）。</summary>
public class SendGridEmailSender(IOptions<SendGridOptions> options) : IEmailSender
{
    /// <inheritdoc/>
    public async Task SendAsync(string to, string subject, string bodyHtml, CancellationToken ct = default)
    {
        var opts = options.Value;
        var client = new SendGridClient(opts.ApiKey);

        var msg = new SendGridMessage
        {
            From        = new EmailAddress(opts.FromAddress),
            Subject     = subject,
            HtmlContent = bodyHtml
        };
        msg.AddTo(new EmailAddress(to));

        var response = await client.SendEmailAsync(msg, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Body.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"SendGrid 寄信失敗 ({(int)response.StatusCode}): {body}");
        }
    }
}
