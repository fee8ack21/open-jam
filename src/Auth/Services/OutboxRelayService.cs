using System.Text.Json;
using Auth.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared.Events;

namespace Auth.Services;

/// <summary>
/// 背景服務，定期掃描 outbox_messages 資料表並將待處理訊息推送至 RabbitMQ。
/// 使用 FOR UPDATE SKIP LOCKED 確保多副本部署時不重複發布。
/// </summary>
public class OutboxRelayService(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxRelayService> logger) : BackgroundService
{
    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "OutboxRelay 發生錯誤");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), ct);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db  = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IBus>();

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var messages = await db.OutboxMessages
            .FromSqlRaw(
                """
                SELECT * FROM outbox_messages
                WHERE processed_at IS NULL
                ORDER BY created_at
                LIMIT 10
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(ct);

        if (messages.Count == 0)
        {
            await tx.RollbackAsync(ct);
            return;
        }

        foreach (var message in messages)
        {
            var evt = JsonSerializer.Deserialize<EmailRequestedEvent>(message.Payload)!;
            await bus.Publish(evt, ct);
            message.ProcessedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        logger.LogInformation("OutboxRelay: 已發布 {Count} 筆訊息", messages.Count);
    }
}
