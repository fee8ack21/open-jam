namespace EmailService.Services.Sending;

/// <summary>信件寄送抽象介面，封裝底層寄信實作（正式環境 SendGrid、地端 SMTP catcher）。</summary>
public interface IEmailSender
{
    /// <summary>發送一封 HTML 信件。</summary>
    /// <param name="to">收件人電子信箱。</param>
    /// <param name="subject">信件主旨。</param>
    /// <param name="bodyHtml">HTML 格式信件內文。</param>
    /// <param name="ct">取消令牌。</param>
    Task SendAsync(string to, string subject, string bodyHtml, CancellationToken ct = default);
}
