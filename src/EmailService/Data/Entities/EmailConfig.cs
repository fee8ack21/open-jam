using Shared.Audit;

namespace EmailService.Data.Entities;

/// <summary>
/// 信件模板設定；以 TemplateKey 識別不同類型的信件，並透過 Translations 支援多語系。
/// </summary>
public class EmailConfig : ICreatedAt
{
    /// <summary>自動遞增識別碼。</summary>
    public int Id { get; set; }

    /// <summary>模板類型鍵值，如 email.verification、email.password_reset（unique）。</summary>
    public string TemplateKey { get; set; } = "";

    /// <summary>模板用途說明（供管理介面顯示）。</summary>
    public string? Description { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>各語系的模板翻譯清單。</summary>
    public ICollection<EmailConfigTranslation> Translations { get; set; } = [];
}
