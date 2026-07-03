using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared.Events;
using StoreService.Data;

namespace StoreService.Consumers;

/// <summary>
/// 消費 UserRegisteredEvent，以信箱回填訪客追蹤紀錄（UserId 為 null）的 UserId。
/// 純冪等 UPDATE，重複投遞無副作用，不需 inbox 去重。
/// </summary>
public class UserRegisteredConsumer(
    StoreDbContext db,
    ILogger<UserRegisteredConsumer> logger) : IConsumer<UserRegisteredEvent>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<UserRegisteredEvent> context)
    {
        var evt = context.Message;
        var ct  = context.CancellationToken;

        var email = evt.Email.Trim().ToLowerInvariant();

        var updated = await db.StoreFollowers
            .Where(f => f.UserId == null && f.Email.ToLower() == email)
            .ExecuteUpdateAsync(s => s.SetProperty(f => f.UserId, evt.UserId), ct);

        if (updated > 0)
            logger.LogInformation(
                "UserRegisteredEvent：回填 {Count} 筆追蹤紀錄的 UserId（使用者 {UserId}）。", updated, evt.UserId);
    }
}
