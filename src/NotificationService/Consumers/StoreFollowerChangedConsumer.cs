using MassTransit;
using Microsoft.EntityFrameworkCore;
using NotificationService.Data;
using NotificationService.Data.Entities;
using Shared.Events;

namespace NotificationService.Consumers;

/// <summary>
/// 消費 StoreFollowerChangedEvent，同步本地 store_follower_ref 參照表（信箱正規化小寫）。
/// 追蹤為 upsert、取消追蹤為 delete，皆為冪等操作，不需 inbox 去重。
/// </summary>
public class StoreFollowerChangedConsumer(
    NotificationDbContext db,
    ILogger<StoreFollowerChangedConsumer> logger) : IConsumer<StoreFollowerChangedEvent>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<StoreFollowerChangedEvent> context)
    {
        var evt = context.Message;
        var ct  = context.CancellationToken;

        var email = evt.Email.Trim().ToLowerInvariant();

        if (evt.Followed)
        {
            var existing = await db.StoreFollowerRefs
                .FirstOrDefaultAsync(f => f.StoreId == evt.StoreId && f.Email == email, ct);

            if (existing is null)
            {
                db.StoreFollowerRefs.Add(new StoreFollowerRef
                {
                    StoreId = evt.StoreId,
                    Email   = email,
                    UserId  = evt.UserId,
                });

                try
                {
                    await db.SaveChangesAsync(ct);
                }
                catch (DbUpdateException ex) when (PostgresErrors.IsUniqueViolation(ex))
                {
                    // 並發重複投遞已插入同一筆，冪等跳過
                    logger.LogDebug("StoreFollowerRef 已存在（{StoreId}, {Email}），略過", evt.StoreId, email);
                }
            }
            else if (evt.UserId is not null && existing.UserId != evt.UserId)
            {
                existing.UserId = evt.UserId;
                await db.SaveChangesAsync(ct);
            }
        }
        else
        {
            await db.StoreFollowerRefs
                .Where(f => f.StoreId == evt.StoreId && f.Email == email)
                .ExecuteDeleteAsync(ct);
        }
    }
}
