using QuotaService.Services.Quotas;

namespace QuotaService.Services.Background;

/// <summary>背景服務，定期釋放逾時仍未 commit 的預扣，回收卡住的儲存空間預扣量。</summary>
public class ReservationSweeperService(
    IServiceScopeFactory scopeFactory,
    ILogger<ReservationSweeperService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(60);

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var quota = scope.ServiceProvider.GetRequiredService<IQuotaManager>();

                var released = await quota.ReleaseExpiredAsync(ct);
                if (released > 0)
                    logger.LogInformation("ReservationSweeper: 已釋放 {Count} 筆逾時預扣", released);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "ReservationSweeper 發生錯誤");
            }

            await Task.Delay(Interval, ct);
        }
    }
}
