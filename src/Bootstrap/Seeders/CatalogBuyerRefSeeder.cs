using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NotificationService.Data;
using NotificationService.Data.Entities;
using OrderService.Data;
using OrderService.Data.Entities;

namespace Bootstrap.Seeders;

/// <summary>
/// 一次性回填 NotificationService 的 catalog_buyer_ref 參照表：
/// 讀取 OrderService 既有完成訂單（OrderCompletedEvent 買家欄位上線前的存量資料），
/// 以 (CatalogId, Email 小寫) upsert（OrderId 取最新完成訂單），重跑冪等。
/// </summary>
public class CatalogBuyerRefSeeder(
    OrderDbContext orderDb,
    NotificationDbContext notificationDb,
    ILogger<CatalogBuyerRefSeeder> logger)
{
    public async Task SeedAsync()
    {
        await notificationDb.Database.MigrateAsync();

        // 每商品每信箱取最新一筆完成訂單（信中下載頁連結的憑證）
        var buyers = await orderDb.Orders.AsNoTracking()
            .Where(o => o.Status == OrderStatus.Completed)
            .OrderBy(o => o.CompletedAt)
            .SelectMany(o => o.Items.Select(i => new
            {
                i.CatalogId,
                o.StoreId,
                o.BuyerEmail,
                o.BuyerUserId,
                OrderId = o.Id,
            }))
            .ToListAsync();

        var latest = new Dictionary<(Guid CatalogId, string Email), (Guid StoreId, Guid? UserId, Guid OrderId)>();
        foreach (var b in buyers)
        {
            var email = b.BuyerEmail.Trim().ToLowerInvariant();
            if (email.Length == 0)
                continue;
            // 依 CompletedAt 升冪走訪，後者覆蓋前者 → 留最新
            latest[(b.CatalogId, email)] = (b.StoreId, b.BuyerUserId, b.OrderId);
        }

        var existing = (await notificationDb.CatalogBuyerRefs.AsNoTracking()
                .Select(r => new { r.CatalogId, r.Email })
                .ToListAsync())
            .Select(r => (r.CatalogId, r.Email))
            .ToHashSet();

        var added = 0;
        foreach (var ((catalogId, email), info) in latest)
        {
            if (existing.Contains((catalogId, email)))
                continue;

            notificationDb.CatalogBuyerRefs.Add(new CatalogBuyerRef
            {
                CatalogId = catalogId,
                StoreId   = info.StoreId,
                Email     = email,
                UserId    = info.UserId,
                OrderId   = info.OrderId,
            });
            added++;
        }

        if (added > 0)
            await notificationDb.SaveChangesAsync();

        logger.LogInformation("Catalog buyer refs seeded：新增 {Added} 筆（來源共 {Total} 組買家）", added, latest.Count);
    }
}
