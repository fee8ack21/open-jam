namespace Shared.Events;

/// <summary>
/// 商店開通事件，由 StoreService 在開店申請核准（建立 Store）的業務 transaction 內寫入 Outbox，
/// 再由排程搬入 RabbitMQ。Auth 消費以將店面子網域（&lt;slug&gt;.openjam.co）的 OIDC
/// redirect / post-logout URI 註冊進 Hydra web client（Hydra 不支援萬用字元 redirect URI）。
/// </summary>
public record StoreProvisionedEvent(
    /// <summary>Outbox 訊息 ID，作為冪等去重鍵。</summary>
    Guid OutboxMessageId,
    /// <summary>商店 ID。</summary>
    Guid StoreId,
    /// <summary>商店子網域 slug（核准後不可變更）。</summary>
    string StoreSlug,
    /// <summary>開通時間（UTC）。</summary>
    DateTimeOffset OccurredAt
);
