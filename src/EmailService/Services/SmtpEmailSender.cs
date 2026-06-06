using EmailService.Options;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;

namespace EmailService.Services;

/// <summary>以 MailKit SMTP 寄信的 IEmailSender 實作（地端開發，對接 SMTP catcher 如 Mailpit）。</summary>
public class SmtpEmailSender(IOptions<SmtpOptions> options) : IEmailSender
{
    /// <inheritdoc/>
    public async Task SendAsync(string to, string subject, string bodyHtml, CancellationToken ct = default)
    {
        var opts = options.Value;

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(opts.FromAddress));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body    = new TextPart("html") { Text = bodyHtml };

        using var client = new SmtpClient();
        await client.ConnectAsync(opts.Host, opts.Port, opts.SecureSocket, ct);

        if (!string.IsNullOrEmpty(opts.Username))
            await client.AuthenticateAsync(opts.Username, opts.Password, ct);

        await client.SendAsync(message, ct);
        await client.DisconnectAsync(quit: true, ct);
    }
}
