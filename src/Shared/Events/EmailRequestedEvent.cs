namespace Shared.Events;

/// <summary>
/// 各服務在業務 transaction 內寫入 Outbox 後，由排程搬入 RabbitMQ 的寄信事件。
/// EmailService Consumer 負責消費、渲染模板並寄出信件。
/// </summary>
public record EmailRequestedEvent(
    /// <summary>Outbox 訊息 ID，作為冪等去重鍵。</summary>
    Guid OutboxMessageId,
    /// <summary>收件人電子信箱。</summary>
    string To,
    /// <summary>信件模板鍵值，如 "email.verification"、"email.password_reset"。</summary>
    string TemplateKey,
    /// <summary>模板渲染參數，鍵為佔位符名稱（如 "VerifyUrl"），值為替換字串。</summary>
    Dictionary<string, string> Params,
    /// <summary>語系代碼，如 "zh-TW"、"en"。</summary>
    string Locale
);
