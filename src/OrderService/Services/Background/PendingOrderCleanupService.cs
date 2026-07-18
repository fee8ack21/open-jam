using OrderService.Services.Orders;

namespace OrderService.Services.Background;

/// <summary>排程取消逾期未付款訂單。Stripe Checkout Session 最長存活 24 小時，逾時的 Pending 訂單
/// 已無法付款，累積會污染買賣家訂單列表；透過與人工取消相同的 expire-first 路徑安全轉為 Cancelled。</summary>
public class PendingOrderCleanupService(
    IServiceScopeFactory scopeFactory,
    ILogger<PendingOrderCleanupService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("PendingOrderCleanupService started.");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var orderManager = scope.ServiceProvider.GetRequiredService<IOrderManager>();

                var cancelled = await orderManager.CleanupExpiredPendingAsync(ct);
                if (cancelled > 0)
                    logger.LogInformation("已自動取消 {Count} 筆逾期未付款訂單。", cancelled);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Pending order cleanup cycle failed.");
            }

            await Task.Delay(Interval, ct);
        }
    }
}
