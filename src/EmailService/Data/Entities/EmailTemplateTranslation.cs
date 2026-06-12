using Shared.Audit;

namespace EmailService.Data.Entities;

/// <summary>
/// 某種信件模板的單一語系版本，包含主旨與 HTML 內文。
/// </summary>
public class EmailTemplateTranslation : ICreatedAt
{
    /// <summary>自動遞增識別碼。</summary>
    public int Id { get; set; }

    /// <summary>關聯的 EmailTemplate ID。</summary>
    public int EmailTemplateId { get; set; }

    /// <summary>語系代碼，如 zh-TW、en。</summary>
    public string Locale { get; set; } = "";

    /// <summary>信件主旨模板，支援 {{Param}} 佔位符。</summary>
    public string Subject { get; set; } = "";

    /// <summary>信件 HTML 內文模板，支援 {{Param}} 佔位符。</summary>
    public string BodyHtml { get; set; } = "";

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>關聯的 EmailTemplate。</summary>
    public EmailTemplate EmailTemplate { get; set; } = null!;
}
