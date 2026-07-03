using System.Text.Json;
using Shared.Events;
using StoreService.Data;
using StoreService.Data.Entities;

namespace StoreService.Services;

/// <summary>
/// 將商店領域事件寫入 Outbox（業務 transaction 內），由 <see cref="Background.OutboxRelayService"/> 搬入 RabbitMQ。
/// </summary>
public class StoreEventPublisher(StoreDbContext db)
{
    /// <summary>商店追蹤關係變更事件的 Outbox EventType。</summary>
    public const string StoreFollowerChangedType = "store.follower_changed";

    /// <summary>寫入一筆 StoreFollowerChangedEvent。不負責 SaveChanges。</summary>
    /// <param name="storeId">商店 ID。</param>
    /// <param name="email">追蹤者信箱。</param>
    /// <param name="userId">追蹤者使用者 ID；訪客為 null。</param>
    /// <param name="followed">true 為追蹤、false 為取消追蹤。</param>
    public void AddStoreFollowerChanged(Guid storeId, string email, Guid? userId, bool followed)
    {
        var outbox = new OutboxMessage { EventType = StoreFollowerChangedType };
        outbox.Payload = JsonSerializer.Serialize(new StoreFollowerChangedEvent(
            OutboxMessageId: outbox.Id,
            StoreId: storeId,
            Email: email,
            UserId: userId,
            Followed: followed,
            OccurredAt: DateTimeOffset.UtcNow));
        db.OutboxMessages.Add(outbox);
    }
}
