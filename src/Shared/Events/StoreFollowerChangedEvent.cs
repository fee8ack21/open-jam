namespace Shared.Events;

/// <summary>
/// 商店追蹤關係變更事件，由 StoreService 在追蹤 / 取消追蹤的業務 transaction 內寫入 Outbox，
/// 再由排程搬入 RabbitMQ。NotificationService 消費以同步本地 store_follower_ref 參照表。
/// </summary>
public record StoreFollowerChangedEvent(
    /// <summary>Outbox 訊息 ID，作為冪等去重鍵。</summary>
    Guid OutboxMessageId,
    /// <summary>商店 ID。</summary>
    Guid StoreId,
    /// <summary>追蹤者電子信箱。</summary>
    string Email,
    /// <summary>追蹤者使用者 ID；null 表示訪客憑信箱追蹤、尚未關聯帳號。</summary>
    Guid? UserId,
    /// <summary>true 為追蹤、false 為取消追蹤。</summary>
    bool Followed,
    /// <summary>變更發生時間（UTC）。</summary>
    DateTimeOffset OccurredAt
);
