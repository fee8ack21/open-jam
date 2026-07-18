using Microsoft.EntityFrameworkCore;
using PaymentService.Data;
using PaymentService.Data.Entities;
using PaymentService.Services.Payments;

namespace PaymentService.Services.Background;

/// <summary>排程處理已落地但尚未完成的 Stripe webhook 事件。事件在 <c>WebhookController</c> 收到時即已落地（含原始 payload），
/// 因此即使本服務在處理過程中重啟 / 掛掉，未完成的事件仍會在下個週期被重新處理，不會遺失。</summary>
public class StripeWebhookProcessorService(
    IServiceScopeFactory scopeFactory,
    ILogger<StripeWebhookProcessorService> logger) : BackgroundService
{
    /// <summary>單一事件的處理重試上限；達上限標記 <c>FailedAt</c> 出列（dead-letter），
    /// 避免毒事件永久佔據批次名額、塞死整條 webhook 管線。</summary>
    private const int MaxAttempts = 5;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("StripeWebhookProcessorService started.");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
                var handler = scope.ServiceProvider.GetRequiredService<StripeWebhookHandler>();

                var batchIds = await db.ProviderEvents
                    .Where(e => e.ProcessedAt == null && e.FailedAt == null)
                    .OrderBy(e => e.CreatedAt)
                    .Take(10)
                    .Select(e => e.Id)
                    .ToListAsync(ct);

                foreach (var id in batchIds)
                {
                    // 每筆事件於迴圈內重新載入：前一筆失敗時的 ChangeTracker.Clear() 會把先前
                    // 查詢載入的實體一併 detach，沿用舊實體會使 ProcessedAt 的變更靜默不落地。
                    var providerEvent = await db.ProviderEvents.FirstOrDefaultAsync(e => e.Id == id, ct);
                    if (providerEvent is null) continue;

                    // 每筆事件獨立提交：單筆失敗（含提交期失敗）只影響自己，不回滾同批其他事件
                    // 的處理結果；失敗事件的半套暫存變更也不會搭其他事件的提交便車落地。
                    try
                    {
                        await handler.ProcessPendingAsync(providerEvent, ct);
                        await db.SaveChangesAsync(ct);
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested)
                    {
                        // 服務關閉中斷不算處理失敗，事件維持未處理、重啟後照常入列。
                        throw;
                    }
                    catch (Exception ex)
                    {
                        db.ChangeTracker.Clear();
                        await RecordFailureAsync(db, providerEvent, ex, ct);
                    }
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Stripe webhook processing cycle failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(3), ct);
        }
    }

    /// <summary>記錄事件處理失敗：累加 AttemptCount、保留最後錯誤，達上限標記 FailedAt 出列並記
    /// error log 供告警。ChangeTracker 已清空，故以不經追蹤的 UPDATE 直接落地。</summary>
    private async Task RecordFailureAsync(
        PaymentDbContext db, ProviderEvent providerEvent, Exception ex, CancellationToken ct)
    {
        var attempt = providerEvent.AttemptCount + 1;
        var failedAt = attempt >= MaxAttempts ? DateTimeOffset.UtcNow : (DateTimeOffset?)null;
        var error = ex.ToString();
        if (error.Length > 2000) error = error[..2000];

        await db.ProviderEvents
            .Where(e => e.Id == providerEvent.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.AttemptCount, attempt)
                .SetProperty(e => e.LastError, error)
                .SetProperty(e => e.FailedAt, failedAt), ct);

        if (failedAt != null)
        {
            logger.LogError(ex,
                "Webhook event {EventId} {EventType} 連續失敗 {Attempt} 次，已標記 dead-letter 出列；" +
                "需人工排查（last_error 欄位）後清除 failed_at 重新入列。",
                providerEvent.EventId, providerEvent.EventType, attempt);
        }
        else
        {
            logger.LogWarning(ex,
                "Failed to process webhook event {EventId} {EventType} (attempt {Attempt}/{Max})",
                providerEvent.EventId, providerEvent.EventType, attempt, MaxAttempts);
        }
    }
}
