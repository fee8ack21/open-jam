using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NotificationService.Data;
using NotificationService.Data.Entities;
using NotificationService.Models;
using NotificationService.Options;
using Shared.Data;
using Shared.Events;

namespace NotificationService.Services.Background;

/// <summary>
/// 背景服務，掃描到期的 Pending 通知任務並對商店追蹤者 fan-out：
/// 已關聯帳號者建立 in-app Notification，全部追蹤者經 Outbox 發 EmailRequestedEvent 由 EmailService 寄信。
/// <para>
/// 整個 fan-out 包在單一交易並以 FOR UPDATE SKIP LOCKED 持鎖，多副本不重複發送；
/// 失敗整體回滾、任務保持 Pending 於下一輪重試（AttemptCount 達上限轉 Failed）。
/// Notification 的 (RequestId, RecipientEmail) 唯一索引為重放時的第二道防線。
/// </para>
/// </summary>
public class NotificationDispatcherService(
    IServiceScopeFactory scopeFactory,
    IOptions<NotificationOptions> options,
    ILogger<NotificationDispatcherService> logger) : BackgroundService
{
    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("NotificationDispatcherService started.");

        while (!ct.IsCancellationRequested)
        {
            var dispatchedAny = false;

            try
            {
                dispatchedAny = await DispatchNextAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "通知任務發送輪詢失敗");
            }

            // 尚有到期任務時立即續跑，否則等待下一輪
            if (!dispatchedAny)
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
        }
    }

    /// <summary>發送下一筆到期任務。回傳是否有處理任務（true 表示可能還有，呼叫端立即續跑）。</summary>
    private async Task<bool> DispatchNextAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db          = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        var storeClient = scope.ServiceProvider.GetRequiredService<StoreServiceClient>();
        var opts        = options.Value;

        // 先無鎖挑出候選任務，把 StoreService HTTP 查詢留在交易外
        var candidate = await db.NotificationRequests.AsNoTracking()
            .Where(r => r.Status == NotificationRequestStatus.Pending
                     && r.ScheduledAt <= DateTimeOffset.UtcNow)
            .OrderBy(r => r.ScheduledAt)
            .FirstOrDefaultAsync(ct);

        if (candidate is null)
            return false;

        StoreServiceClient.StoreInfo? store = null;
        string? storeError = null;
        try
        {
            store = await storeClient.GetStoreAsync(candidate.StoreId, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            storeError = $"查詢商店資訊失敗：{ex.Message}";
        }

        await db.Database.ExecuteInTransactionAsync(async tx =>
        {
            // 重試 execution strategy 會整段重放，先清空前次嘗試殘留的追蹤實體
            db.ChangeTracker.Clear();

            // 持鎖 re-claim；已被其他副本處理（鎖住或狀態改變）則跳過
            var request = (await db.NotificationRequests
                .FromSql($"""
                    SELECT * FROM notification_requests
                    WHERE id = {candidate.Id}
                    FOR UPDATE SKIP LOCKED
                    """)
                .ToListAsync(ct)).FirstOrDefault();

            if (request is null
                || request.Status != NotificationRequestStatus.Pending
                || request.ScheduledAt > DateTimeOffset.UtcNow)
            {
                await tx.RollbackAsync(ct);
                return;
            }

            request.AttemptCount++;

            if (store is null)
            {
                request.LastError = storeError;
                if (request.AttemptCount >= opts.DispatchMaxAttempts)
                {
                    request.Status = NotificationRequestStatus.Failed;
                    logger.LogError("通知任務 {RequestId} 重試 {Attempts} 次仍失敗，放棄：{Error}",
                        request.Id, request.AttemptCount, storeError);
                }

                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                return;
            }

            var count = await FanOutAsync(db, request, store, opts, ct);

            request.Status       = NotificationRequestStatus.Dispatched;
            request.DispatchedAt = DateTimeOffset.UtcNow;
            request.LastError    = null;

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            logger.LogInformation("通知任務 {RequestId}（{Type}，商店 {StoreId}）已發送給 {Count} 位追蹤者。",
                request.Id, request.Type, request.StoreId, count);
        }, ct);

        return true;
    }

    /// <summary>
    /// 依通知類型 fan-out：catalog.version_released 對商品既有買家，其餘對商店追蹤者。
    /// 回傳通知的收件者數。不負責 Commit。
    /// </summary>
    private static Task<int> FanOutAsync(
        NotificationDbContext db,
        NotificationRequest request,
        StoreServiceClient.StoreInfo store,
        NotificationOptions opts,
        CancellationToken ct) =>
        request.Type == NotificationTypes.CatalogVersionReleased
            ? FanOutBuyersAsync(db, request, store, opts, ct)
            : FanOutFollowersAsync(db, request, store, opts, ct);

    /// <summary>對商店追蹤者分批 fan-out。回傳通知的追蹤者數。不負責 Commit。</summary>
    private static async Task<int> FanOutFollowersAsync(
        NotificationDbContext db,
        NotificationRequest request,
        StoreServiceClient.StoreInfo store,
        NotificationOptions opts,
        CancellationToken ct)
    {
        var inAppPayload  = BuildInAppPayload(request, store);
        // "catalog.published" → 模板鍵 "notification.catalog_published"
        var templateKey   = "notification." + request.Type.Replace('.', '_');
        var emailParams   = BuildEmailParams(request, store, opts);

        var total     = 0;
        var lastEmail = "";

        while (true)
        {
            var batch = await db.StoreFollowerRefs.AsNoTracking()
                .Where(f => f.StoreId == request.StoreId && string.Compare(f.Email, lastEmail) > 0)
                .OrderBy(f => f.Email)
                .Take(opts.DispatchBatchSize)
                .ToListAsync(ct);

            if (batch.Count == 0)
                break;

            foreach (var follower in batch)
            {
                if (follower.UserId is { } userId)
                {
                    db.Notifications.Add(new Notification
                    {
                        RequestId       = request.Id,
                        RecipientUserId = userId,
                        RecipientEmail  = follower.Email,
                        Type            = request.Type,
                        Payload         = inAppPayload,
                    });
                }

                var outbox = new OutboxMessage { EventType = "email." + request.Type };
                outbox.Payload = JsonSerializer.Serialize(new EmailRequestedEvent(
                    OutboxMessageId: outbox.Id,
                    To:              follower.Email,
                    TemplateKey:     templateKey,
                    Params:          emailParams,
                    Locale:          "zh-TW"));
                db.OutboxMessages.Add(outbox);
            }

            // 交易內分批落地，避免一次追蹤過多實體
            await db.SaveChangesAsync(ct);

            total     += batch.Count;
            lastEmail  = batch[^1].Email;
        }

        return total;
    }

    /// <summary>
    /// 對商品既有買家（CatalogBuyerRef）分批 fan-out。信件的下載頁連結以各買家自己的
    /// 完成訂單 ID 組出（訪客亦可憑此下載），故 download_url 為逐收件者參數。
    /// 回傳通知的買家數。不負責 Commit。
    /// </summary>
    private static async Task<int> FanOutBuyersAsync(
        NotificationDbContext db,
        NotificationRequest request,
        StoreServiceClient.StoreInfo store,
        NotificationOptions opts,
        CancellationToken ct)
    {
        var payload = JsonSerializer.Deserialize<CatalogVersionReleasedPayload>(request.Payload, PayloadJson.Options)!;

        var inAppPayload = BuildInAppPayload(request, store);
        // "catalog.version_released" → 模板鍵 "notification.catalog_version_released"
        var templateKey  = "notification." + request.Type.Replace('.', '_');
        var baseParams   = BuildEmailParams(request, store, opts);

        var total     = 0;
        var lastEmail = "";

        while (true)
        {
            var batch = await db.CatalogBuyerRefs.AsNoTracking()
                .Where(b => b.CatalogId == payload.CatalogId && string.Compare(b.Email, lastEmail) > 0)
                .OrderBy(b => b.Email)
                .Take(opts.DispatchBatchSize)
                .ToListAsync(ct);

            if (batch.Count == 0)
                break;

            foreach (var buyer in batch)
            {
                if (buyer.UserId is { } userId)
                {
                    db.Notifications.Add(new Notification
                    {
                        RequestId       = request.Id,
                        RecipientUserId = userId,
                        RecipientEmail  = buyer.Email,
                        Type            = request.Type,
                        Payload         = inAppPayload,
                    });
                }

                var emailParams = new Dictionary<string, string>(baseParams)
                {
                    ["download_url"] = opts.OrderUrlPattern
                        .Replace("{storeSlug}", store.StoreSlug)
                        .Replace("{orderId}", buyer.OrderId.ToString()),
                };

                var outbox = new OutboxMessage { EventType = "email." + request.Type };
                outbox.Payload = JsonSerializer.Serialize(new EmailRequestedEvent(
                    OutboxMessageId: outbox.Id,
                    To:              buyer.Email,
                    TemplateKey:     templateKey,
                    Params:          emailParams,
                    Locale:          "zh-TW"));
                db.OutboxMessages.Add(outbox);
            }

            // 交易內分批落地，避免一次追蹤過多實體
            await db.SaveChangesAsync(ct);

            total     += batch.Count;
            lastEmail  = batch[^1].Email;
        }

        return total;
    }

    /// <summary>把商店資訊補進任務 Payload，作為 in-app 通知內容。</summary>
    private static string BuildInAppPayload(NotificationRequest request, StoreServiceClient.StoreInfo store)
    {
        switch (request.Type)
        {
            case NotificationTypes.CatalogPublished:
            {
                var payload = JsonSerializer.Deserialize<CatalogPublishedPayload>(request.Payload, PayloadJson.Options)!;
                payload.StoreName = store.StoreName;
                payload.StoreSlug = store.StoreSlug;
                return JsonSerializer.Serialize(payload, PayloadJson.Options);
            }
            case NotificationTypes.StoreAnnouncement:
            {
                var payload = JsonSerializer.Deserialize<StoreAnnouncementPayload>(request.Payload, PayloadJson.Options)!;
                payload.StoreName = store.StoreName;
                payload.StoreSlug = store.StoreSlug;
                return JsonSerializer.Serialize(payload, PayloadJson.Options);
            }
            case NotificationTypes.CatalogVersionReleased:
            {
                var payload = JsonSerializer.Deserialize<CatalogVersionReleasedPayload>(request.Payload, PayloadJson.Options)!;
                payload.StoreName = store.StoreName;
                payload.StoreSlug = store.StoreSlug;
                return JsonSerializer.Serialize(payload, PayloadJson.Options);
            }
            default:
                throw new InvalidOperationException($"不支援的通知類型 '{request.Type}'");
        }
    }

    /// <summary>組出信件模板渲染參數。</summary>
    private static Dictionary<string, string> BuildEmailParams(
        NotificationRequest request,
        StoreServiceClient.StoreInfo store,
        NotificationOptions opts)
    {
        switch (request.Type)
        {
            case NotificationTypes.CatalogPublished:
            {
                var payload = JsonSerializer.Deserialize<CatalogPublishedPayload>(request.Payload, PayloadJson.Options)!;
                var catalogUrl = opts.CatalogUrlPattern
                    .Replace("{storeSlug}", store.StoreSlug)
                    .Replace("{catalogId}", payload.CatalogId.ToString());

                return new()
                {
                    ["store_name"]   = store.StoreName,
                    ["store_slug"]   = store.StoreSlug,
                    ["catalog_name"] = payload.CatalogName,
                    ["catalog_url"]  = catalogUrl,
                    ["price"]        = payload.Price.ToString("0.##"),
                    ["currency"]     = payload.Currency,
                };
            }
            case NotificationTypes.StoreAnnouncement:
            {
                var payload = JsonSerializer.Deserialize<StoreAnnouncementPayload>(request.Payload, PayloadJson.Options)!;
                return new()
                {
                    ["store_name"] = store.StoreName,
                    ["store_slug"] = store.StoreSlug,
                    ["title"]      = payload.Title,
                    ["message"]    = payload.Message,
                };
            }
            case NotificationTypes.CatalogVersionReleased:
            {
                // download_url 依收件者訂單而異，由 FanOutBuyersAsync 逐收件者補上
                var payload = JsonSerializer.Deserialize<CatalogVersionReleasedPayload>(request.Payload, PayloadJson.Options)!;
                var catalogUrl = opts.CatalogUrlPattern
                    .Replace("{storeSlug}", store.StoreSlug)
                    .Replace("{catalogId}", payload.CatalogId.ToString());

                return new()
                {
                    ["store_name"]   = store.StoreName,
                    ["store_slug"]   = store.StoreSlug,
                    ["catalog_name"] = payload.CatalogName,
                    ["catalog_url"]  = catalogUrl,
                    ["version"]      = payload.Version,
                    ["release_note"] = payload.ReleaseNote ?? "",
                };
            }
            default:
                throw new InvalidOperationException($"不支援的通知類型 '{request.Type}'");
        }
    }
}
