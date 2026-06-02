namespace EmailService.Services;

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string bodyHtml, CancellationToken ct = default);
}
