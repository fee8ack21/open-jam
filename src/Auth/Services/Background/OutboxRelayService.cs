using System.Text.Json;
using Auth.Data;
using Auth.Data.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared.Events;

namespace Auth.Services.Background;

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
            var evt = DeserializeEvent(message);
            if (evt is null)
            {
                // 標記已處理避免卡住整批掃描；未知型別代表程式漏了對應的 case
                logger.LogError("OutboxRelay: 不支援的事件型別 {EventType}（訊息 {MessageId}），已跳過", message.EventType, message.Id);
                message.ProcessedAt = DateTimeOffset.UtcNow;
                continue;
            }

            await bus.Publish(evt, ct);
            message.ProcessedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        logger.LogInformation("OutboxRelay: 已發布 {Count} 筆訊息", messages.Count);
    }

    /// <summary>依 EventType 將 payload 反序列化為對應事件型別；新增事件類別時需在此補上 case。</summary>
    private static object? DeserializeEvent(OutboxMessage message) => message.EventType switch
    {
        var t when t.StartsWith("email.") => JsonSerializer.Deserialize<EmailRequestedEvent>(message.Payload),
        _ => null,
    };
}
