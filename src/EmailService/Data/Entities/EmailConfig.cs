namespace EmailService.Data.Entities;

public class EmailConfig
{
    public int     Id          { get; set; }
    public string  TemplateKey { get; set; } = "";
    public string? Description { get; set; }
    public DateTime CreatedAt  { get; set; }

    public ICollection<EmailConfigTranslation> Translations { get; set; } = [];
}
