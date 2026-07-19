namespace Shared.Outbox;

/// <summary>
/// Outbox 訊息共同介面；各服務的 <c>OutboxMessage</c> entity 實作後即可交由
/// <see cref="OutboxRelayServiceBase{TDbContext, TMessage}"/> 統一搬運。
/// </summary>
public interface IOutboxMessage
{
    /// <summary>訊息唯一識別碼（下游消費端的冪等去重鍵）。</summary>
    Guid Id { get; }

    /// <summary>事件類型，如 email.verification、audit.store.created。</summary>
    string EventType { get; }

    /// <summary>JSON 序列化的事件 payload。</summary>
    string Payload { get; }

    /// <summary>訊息推送至 MQ 的時間；null 表示尚未處理。</summary>
    DateTimeOffset? ProcessedAt { get; set; }

    /// <summary>推送失敗累計次數，供指數退避與毒訊息隔離判斷。</summary>
    int AttemptCount { get; set; }

    /// <summary>失敗退避後下次可重試的時間；null 表示可立即處理。</summary>
    DateTimeOffset? NextAttemptAt { get; set; }

    /// <summary>反序列化失敗達上限被隔離的時間；非 null 者不再撈取，需人工介入。</summary>
    DateTimeOffset? FailedAt { get; set; }

    /// <summary>最近一次失敗的錯誤訊息，成功後清除。</summary>
    string? LastError { get; set; }
}
