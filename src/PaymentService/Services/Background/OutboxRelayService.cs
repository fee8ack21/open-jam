using System.Text.Json;
using MassTransit;
using PaymentService.Data;
using PaymentService.Data.Entities;
using Shared.Data;
using Shared.Events;

namespace PaymentService.Services.Background;

public class OutboxRelayService(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxRelayService> logger) : BackgroundService
{

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("OutboxRelayService started.");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
                var bus = scope.ServiceProvider.GetRequiredService<IBus>();

                await db.Database.ExecuteInTransactionAsync(async tx =>
                {
                    var batch = db.OutboxMessages
                        .Where(m => m.ProcessedAt == null)
                        .OrderBy(m => m.CreatedAt)
                        .Take(10)
                        .ToList();

                    foreach (var msg in batch)
                    {
                        try
                        {
                            if (msg.EventType.StartsWith("email."))
                            {
                                var evt = JsonSerializer.Deserialize<EmailRequestedEvent>(msg.Payload, OutboxJson.Options);
                                if (evt != null) await bus.Publish(evt, ct);
                            }
                            else if (msg.EventType.StartsWith("audit."))
                            {
                                var evt = JsonSerializer.Deserialize<AuditLogRequestedEvent>(msg.Payload, OutboxJson.Options);
                                if (evt != null) await bus.Publish(evt, ct);
                            }
                            else if (msg.EventType.StartsWith("payment."))
                            {
                                var evt = JsonSerializer.Deserialize<PaymentSucceededEvent>(msg.Payload, OutboxJson.Options);
                                if (evt != null) await bus.Publish(evt, ct);
                            }

                            msg.ProcessedAt = DateTimeOffset.UtcNow;
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex, "Failed to relay outbox message {Id} type {Type}", msg.Id, msg.EventType);
                        }
                    }

                    await db.SaveChangesAsync(CancellationToken.None);
                    await tx.CommitAsync(ct);
                }, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox relay cycle failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), ct);
        }
    }
}
