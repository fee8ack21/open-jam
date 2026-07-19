using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Data;

namespace Shared.Outbox;

/// <summary>
/// Outbox relay 共用基底：定期掃描 outbox_messages 資料表並將待處理訊息推送至 RabbitMQ。
/// <para>以 <c>FOR UPDATE SKIP LOCKED</c> 撈批，確保多副本部署時各副本取得不相交批次、不重複發布。</para>
/// <para>撈滿一批立即續撈（drain loop）以消化積壓，撈不滿才回到 <see cref="PollInterval"/> 輪詢，
/// 佇列清空時的背景負載與固定輪詢相同。</para>
/// <para>單筆失敗不影響同批其他訊息：累計 <see cref="IOutboxMessage.AttemptCount"/> 並指數退避
/// （上限 <see cref="MaxBackoff"/>）。反序列化失敗屬確定性錯誤（毒訊息），重試
/// <see cref="PoisonMaxAttempts"/> 次（涵蓋 rolling deploy 新舊版本交接窗）後標記
/// <see cref="IOutboxMessage.FailedAt"/> 隔離、不阻塞佇列；publish 失敗視為暫時性（broker 斷線等），
/// 退避後永久重試、不自動隔離，避免 broker 長時間停機誤殺正常訊息。</para>
/// </summary>
public abstract class OutboxRelayServiceBase<TDbContext, TMessage>(
    IServiceScopeFactory scopeFactory,
    ILogger logger) : BackgroundService
    where TDbContext : DbContext
    where TMessage : class, IOutboxMessage
{
    /// <summary>單批撈取筆數，亦為 drain loop「撈滿續撈」的門檻。</summary>
    protected virtual int BatchSize => 100;

    /// <summary>佇列清空後的輪詢間隔，同時是失敗退避的基準單位。</summary>
    protected virtual TimeSpan PollInterval => TimeSpan.FromSeconds(5);

    /// <summary>單筆失敗指數退避的上限。</summary>
    protected virtual TimeSpan MaxBackoff => TimeSpan.FromMinutes(10);

    /// <summary>毒訊息（反序列化失敗 / 未知型別）標記 Failed 前的重試次數。</summary>
    protected virtual int PoisonMaxAttempts => 5;

    /// <summary>
    /// 依 EventType 將 payload 反序列化為對應事件型別；新增事件類別時需在子類補上 case。
    /// 回傳 null 表示未知型別，比照反序列化失敗以毒訊息處理。
    /// 序列化設定須與該服務 Publisher 寫入端一致（見 <c>OutboxJson</c> 說明）。
    /// </summary>
    protected abstract object? DeserializeEvent(TMessage message);

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var fullBatch = false;
            try
            {
                fullBatch = await ProcessBatchAsync(ct) >= BatchSize;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "OutboxRelay 發生錯誤");
            }

            if (!fullBatch)
            {
                await Task.Delay(PollInterval, ct);
            }
        }
    }

    /// <summary>處理一批訊息，回傳本批撈取筆數（含失敗者），供 drain loop 判斷是否續撈。</summary>
    private async Task<int> ProcessBatchAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db  = scope.ServiceProvider.GetRequiredService<TDbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IBus>();

        // 啟用 EnableRetryOnFailure 後，顯式交易須包進 execution strategy 才能在 transient 失敗時整體重放。
        return await db.Database.ExecuteInTransactionAsync(async tx =>
        {
            // BatchSize 為程式內常數、非外部輸入，直接內插無注入疑慮。
            var sql = $"""
                SELECT * FROM outbox_messages
                WHERE processed_at IS NULL AND failed_at IS NULL
                  AND (next_attempt_at IS NULL OR next_attempt_at <= now())
                ORDER BY created_at
                LIMIT {BatchSize}
                FOR UPDATE SKIP LOCKED
                """;
            var messages = await db.Set<TMessage>().FromSqlRaw(sql).ToListAsync(ct);

            if (messages.Count == 0)
            {
                await tx.RollbackAsync(ct);
                return 0;
            }

            var published = 0;
            foreach (var message in messages)
            {
                try
                {
                    var evt = DeserializeEvent(message);
                    if (evt is null)
                    {
                        MarkFailure(message, poison: true, null, "未知事件型別，缺少對應的反序列化 case");
                        continue;
                    }

                    await bus.Publish(evt, ct);
                    message.ProcessedAt = DateTimeOffset.UtcNow;
                    message.LastError = null;
                    published++;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    MarkFailure(message, poison: ex is JsonException, ex, ex.Message);
                }
            }

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            if (published > 0)
            {
                logger.LogInformation("OutboxRelay: 已發布 {Count} 筆訊息", published);
            }

            return messages.Count;
        }, ct);
    }

    /// <summary>
    /// 累計失敗次數並排定退避。毒訊息達上限即標記 Failed 隔離，
    /// 需人工介入（修正資料或補程式後清除 FailedAt / AttemptCount 重新投遞）。
    /// </summary>
    private void MarkFailure(TMessage message, bool poison, Exception? exception, string error)
    {
        message.AttemptCount++;
        message.LastError = error.Length > 2000 ? error[..2000] : error;

        if (poison && message.AttemptCount >= PoisonMaxAttempts)
        {
            message.FailedAt = DateTimeOffset.UtcNow;
            logger.LogError(exception,
                "OutboxRelay: 訊息 {MessageId}（{EventType}）反序列化失敗達 {Attempts} 次，已標記 Failed 隔離",
                message.Id, message.EventType, message.AttemptCount);
            return;
        }

        var backoff = TimeSpan.FromTicks(Math.Min(
            PollInterval.Ticks * (1L << Math.Min(message.AttemptCount - 1, 20)),
            MaxBackoff.Ticks));
        message.NextAttemptAt = DateTimeOffset.UtcNow + backoff;
        logger.LogWarning(exception,
            "OutboxRelay: 訊息 {MessageId}（{EventType}）第 {Attempts} 次處理失敗，{Backoff} 後重試",
            message.Id, message.EventType, message.AttemptCount, backoff);
    }
}
