namespace EmailService.Options;

/// <summary>SMTP 連線設定。</summary>
public class SmtpOptions
{
    /// <summary>SMTP 主機位址。</summary>
    /// <example>smtp.gmail.com</example>
    public string Host { get; set; } = "localhost";

    /// <summary>SMTP 連接埠。</summary>
    /// <example>587</example>
    public int Port { get; set; } = 587;

    /// <summary>是否使用 SSL/TLS。</summary>
    /// <example>false</example>
    public bool UseSsl { get; set; } = false;

    /// <summary>SMTP 認證帳號；本地開發可留空。</summary>
    /// <example>no-reply@openjam.co</example>
    public string Username { get; set; } = "";

    /// <summary>SMTP 認證密碼；正式環境應透過 Secret 注入。</summary>
    public string Password { get; set; } = "";

    /// <summary>寄件人地址。</summary>
    /// <example>noreply@openjam.co</example>
    public string FromAddress { get; set; } = "";
}
