using Shared.Audit;

namespace EmailService.Data.Entities;

/// <summary>信件寄送狀態。</summary>
public enum EmailStatus
{
    /// <summary>已排入待發送。</summary>
    Pending,

    /// <summary>已成功寄出。</summary>
    Sent,

    /// <summary>發送失敗，等待重試或達上限。</summary>
    Failed,
}

/// <summary>
/// 每封信的寄送紀錄；以 OutboxMessageId 作為去重鍵確保冪等。
/// </summary>
public class EmailRecord : ICreatedAt, IUpdatedAt
{
    /// <summary>紀錄唯一識別碼。</summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>來源 Outbox 訊息 ID，同時作為去重鍵（unique index）。</summary>
    public Guid OutboxMessageId { get; set; }

    /// <summary>收件人電子信箱。</summary>
    public string To { get; set; } = "";

    /// <summary>信件主旨（渲染後）。</summary>
    public string Subject { get; set; } = "";

    /// <summary>信件 HTML 內容（渲染後）。</summary>
    public string BodyHtml { get; set; } = "";

    /// <summary>目前寄送狀態。</summary>
    public EmailStatus Status { get; set; }

    /// <summary>總嘗試次數。</summary>
    public int AttemptCount { get; set; }

    /// <summary>最後一次嘗試發送的時間。</summary>
    public DateTimeOffset? LastAttemptAt { get; set; }

    /// <summary>成功寄出的時間；null 表示尚未成功。</summary>
    public DateTimeOffset? SentAt { get; set; }

    /// <summary>最後一次失敗的錯誤訊息。</summary>
    public string? ErrorMessage { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <inheritdoc/>
    public DateTimeOffset? UpdatedAt { get; private set; }
}
