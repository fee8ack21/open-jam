using MassTransit;
using Microsoft.EntityFrameworkCore;
using NotificationService.Data;
using Shared.Events;

namespace NotificationService.Consumers;

/// <summary>
/// 消費 UserRegisteredEvent，以信箱回填訪客追蹤參照（UserId 為 null）的 UserId，
/// 使其後續通知可建立 in-app 紀錄。純冪等 UPDATE，重複投遞無副作用，不需 inbox 去重。
/// </summary>
public class UserRegisteredConsumer(
    NotificationDbContext db,
    ILogger<UserRegisteredConsumer> logger) : IConsumer<UserRegisteredEvent>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<UserRegisteredEvent> context)
    {
        var evt = context.Message;
        var ct  = context.CancellationToken;

        var email = evt.Email.Trim().ToLowerInvariant();

        var updated = await db.StoreFollowerRefs
            .Where(f => f.UserId == null && f.Email == email)
            .ExecuteUpdateAsync(s => s.SetProperty(f => f.UserId, evt.UserId), ct);

        if (updated > 0)
            logger.LogInformation(
                "UserRegisteredEvent：回填 {Count} 筆追蹤參照的 UserId（使用者 {UserId}）。", updated, evt.UserId);
    }
}
