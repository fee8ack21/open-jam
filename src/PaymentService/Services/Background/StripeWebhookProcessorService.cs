using Microsoft.EntityFrameworkCore;
using PaymentService.Data;
using PaymentService.Services.Payments;

namespace PaymentService.Services.Background;

/// <summary>排程處理已落地但尚未完成的 Stripe webhook 事件。事件在 <c>WebhookController</c> 收到時即已落地（含原始 payload），
/// 因此即使本服務在處理過程中重啟 / 掛掉，未完成的事件仍會在下個週期被重新處理，不會遺失。</summary>
public class StripeWebhookProcessorService(
    IServiceScopeFactory scopeFactory,
    ILogger<StripeWebhookProcessorService> logger) : BackgroundService
{
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

                var batch = await db.ProviderEvents
                    .Where(e => e.ProcessedAt == null)
                    .OrderBy(e => e.CreatedAt)
                    .Take(10)
                    .ToListAsync(ct);

                foreach (var providerEvent in batch)
                {
                    try
                    {
                        await handler.ProcessPendingAsync(providerEvent, ct);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to process webhook event {EventId} {EventType}",
                            providerEvent.EventId, providerEvent.EventType);
                    }
                }

                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Stripe webhook processing cycle failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(3), ct);
        }
    }
}
