using MassTransit;
using Microsoft.EntityFrameworkCore;
using NotificationService.Data;
using NotificationService.Data.Entities;
using Shared.Data;
using Shared.Events;

namespace NotificationService.Consumers;

/// <summary>
/// 消費 OrderCompletedEvent，維護商品買家參照表（CatalogBuyerRef）：訂單內每個商品
/// upsert 一筆（每商品每信箱一筆），OrderId 更新為最新完成訂單、UserId 有值即補上。
/// 天然冪等 upsert，重複投遞無副作用，不需 inbox 去重；並發撞唯一索引時回滾重試。
/// </summary>
public class OrderCompletedConsumer(
    NotificationDbContext db,
    ILogger<OrderCompletedConsumer> logger) : IConsumer<OrderCompletedEvent>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<OrderCompletedEvent> context)
    {
        var evt = context.Message;
        var ct  = context.CancellationToken;

        var email = evt.BuyerEmail.Trim().ToLowerInvariant();
        if (email.Length == 0)
            return;

        var catalogIds = evt.Items.Select(i => i.CatalogId).Distinct().ToList();

        await db.Database.ExecuteInTransactionAsync(async tx =>
        {
            // 重試 execution strategy 會整段重放，先清空前次嘗試殘留的追蹤實體
            db.ChangeTracker.Clear();

            var existing = await db.CatalogBuyerRefs
                .Where(b => b.Email == email && catalogIds.Contains(b.CatalogId))
                .ToDictionaryAsync(b => b.CatalogId, ct);

            foreach (var catalogId in catalogIds)
            {
                if (existing.TryGetValue(catalogId, out var buyerRef))
                {
                    buyerRef.OrderId = evt.OrderId;
                    buyerRef.UserId ??= evt.BuyerUserId;
                }
                else
                {
                    db.CatalogBuyerRefs.Add(new CatalogBuyerRef
                    {
                        CatalogId = catalogId,
                        StoreId   = evt.StoreId,
                        Email     = email,
                        UserId    = evt.BuyerUserId,
                        OrderId   = evt.OrderId,
                    });
                }
            }

            try
            {
                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                logger.LogInformation(
                    "OrderCompletedEvent：訂單 {OrderId} 買家參照已同步（{Count} 個商品）。",
                    evt.OrderId, catalogIds.Count);
            }
            catch (DbUpdateException ex) when (PostgresErrors.IsUniqueViolation(ex))
            {
                // 同信箱同商品並發插入撞唯一索引 → 回滾，交由 MassTransit 重試走 update 路徑
                await tx.RollbackAsync(ct);
                throw;
            }
        }, ct);
    }
}
