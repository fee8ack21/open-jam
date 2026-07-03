namespace Shared.Events;

/// <summary>
/// 使用者註冊完成事件，由 Auth 在信箱驗證成功（帳號轉為 Active）的 transaction 內寫入 Outbox，
/// 再由排程搬入 RabbitMQ。以驗證成功為準確保信箱所有權已證明。
/// StoreService / NotificationService 各自消費，以信箱回填訪客追蹤紀錄的 UserId。
/// </summary>
public record UserRegisteredEvent(
    /// <summary>Outbox 訊息 ID，作為冪等去重鍵。</summary>
    Guid OutboxMessageId,
    /// <summary>使用者 ID。</summary>
    Guid UserId,
    /// <summary>使用者電子信箱（已正規化小寫）。</summary>
    string Email,
    /// <summary>註冊完成（信箱驗證成功）時間（UTC）。</summary>
    DateTimeOffset RegisteredAt
);
