using QuotaService.Services.Quotas;

namespace QuotaService.Services.Background;

/// <summary>
/// 每日對帳背景服務：向 StorageService 加總每位租戶的實際檔案用量，校正 used 計數器漂移。
/// 不一致時以實際用量為準。
/// </summary>
public class ReconciliationService(
    IServiceScopeFactory scopeFactory,
    ILogger<ReconciliationService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // 啟動後先等一個週期，避免冷啟動即打 StorageService。
        try { await Task.Delay(Interval, ct); }
        catch (OperationCanceledException) { return; }

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await ReconcileAllAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Reconciliation 發生錯誤");
            }

            await Task.Delay(Interval, ct);
        }
    }

    private async Task ReconcileAllAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var quota   = scope.ServiceProvider.GetRequiredService<IQuotaManager>();
        var storage = scope.ServiceProvider.GetRequiredService<StorageServiceClient>();

        var tenantIds = await quota.ListTenantIdsAsync(ct);
        var corrected = 0;

        foreach (var tenantId in tenantIds)
        {
            var actual = await storage.GetTenantUsageBytesAsync(tenantId, ct);
            await quota.ReconcileUsedAsync(tenantId, actual, ct);
            corrected++;
        }

        logger.LogInformation("Reconciliation: 已對帳 {Count} 位租戶", corrected);
    }
}
