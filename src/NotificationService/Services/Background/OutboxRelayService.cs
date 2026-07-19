using System.Text.Json;
using NotificationService.Data;
using NotificationService.Data.Entities;
using Shared.Events;
using Shared.Outbox;

namespace NotificationService.Services.Background;

/// <summary>Outbox relay；掃描與推送邏輯見 <see cref="OutboxRelayServiceBase{TDbContext, TMessage}"/>。</summary>
public class OutboxRelayService(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxRelayService> logger) : OutboxRelayServiceBase<NotificationDbContext, OutboxMessage>(scopeFactory, logger)
{
    /// <inheritdoc/>
    protected override object? DeserializeEvent(OutboxMessage message) =>
        // 目前 NotificationService 僅發布寄信事件。
        JsonSerializer.Deserialize<EmailRequestedEvent>(message.Payload);
}
