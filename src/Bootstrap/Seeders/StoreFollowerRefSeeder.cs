using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NotificationService.Data;
using NotificationService.Data.Entities;
using StoreService.Data;

namespace Bootstrap.Seeders;

/// <summary>
/// 一次性回填 NotificationService 的 store_follower_ref 參照表：
/// 讀取 StoreService 既有追蹤者（StoreFollowerChangedEvent 上線前的存量資料），
/// 以 (StoreId, Email 小寫) upsert，重跑冪等。
/// </summary>
public class StoreFollowerRefSeeder(
    StoreDbContext storeDb,
    NotificationDbContext notificationDb,
    ILogger<StoreFollowerRefSeeder> logger)
{
    public async Task SeedAsync()
    {
        await notificationDb.Database.MigrateAsync();
        logger.LogInformation("Notification DB migrations applied");

        var followers = await storeDb.StoreFollowers.AsNoTracking().ToListAsync();

        var existing = (await notificationDb.StoreFollowerRefs.AsNoTracking()
                .Select(f => new { f.StoreId, f.Email })
                .ToListAsync())
            .Select(f => (f.StoreId, f.Email))
            .ToHashSet();

        var added = 0;
        foreach (var follower in followers)
        {
            var email = follower.Email.Trim().ToLowerInvariant();
            if (existing.Contains((follower.StoreId, email)))
                continue;

            notificationDb.StoreFollowerRefs.Add(new StoreFollowerRef
            {
                StoreId = follower.StoreId,
                Email   = email,
                UserId  = follower.UserId,
            });
            added++;
        }

        if (added > 0)
            await notificationDb.SaveChangesAsync();

        logger.LogInformation("Store follower refs seeded：新增 {Added} 筆（來源共 {Total} 筆）", added, followers.Count);
    }
}
