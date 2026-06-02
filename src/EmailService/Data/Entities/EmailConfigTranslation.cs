namespace EmailService.Data.Entities;

public class EmailConfigTranslation
{
    public int    Id            { get; set; }
    public int    EmailConfigId { get; set; }
    public string Locale        { get; set; } = "";
    public string Subject       { get; set; } = "";
    public string BodyHtml      { get; set; } = "";
    public DateTime CreatedAt   { get; set; }

    public EmailConfig EmailConfig { get; set; } = null!;
}
