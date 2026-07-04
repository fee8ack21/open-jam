using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using Shared.Events;

namespace OrderService.Consumers;

/// <summary>
/// 消費 UserRegisteredEvent，以信箱回填訪客訂單（BuyerUserId 為 null）的 BuyerUserId，
/// 讓訪客購買後註冊即可在會員中心查看購買紀錄與下載。
/// 純冪等 UPDATE，重複投遞無副作用，不需 inbox 去重。
/// </summary>
public class UserRegisteredConsumer(
    OrderDbContext db,
    ILogger<UserRegisteredConsumer> logger) : IConsumer<UserRegisteredEvent>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<UserRegisteredEvent> context)
    {
        var evt = context.Message;
        var ct  = context.CancellationToken;

        var email = evt.Email.Trim().ToLowerInvariant();

        var updated = await db.Orders
            .Where(o => o.BuyerUserId == null && o.BuyerEmail.ToLower() == email)
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.BuyerUserId, evt.UserId), ct);

        if (updated > 0)
            logger.LogInformation(
                "UserRegisteredEvent：回填 {Count} 筆訪客訂單的 BuyerUserId（使用者 {UserId}）。", updated, evt.UserId);
    }
}
