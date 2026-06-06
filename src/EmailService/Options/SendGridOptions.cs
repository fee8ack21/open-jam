namespace EmailService.Options;

/// <summary>SendGrid API 設定（正式環境）。</summary>
public class SendGridOptions
{
    /// <summary>SendGrid API Key；由 GCP Secret Manager 注入，不可留空。</summary>
    public string ApiKey { get; set; } = "";

    /// <summary>寄件人地址。</summary>
    /// <example>noreply@openjam.co</example>
    public string FromAddress { get; set; } = "";
}
