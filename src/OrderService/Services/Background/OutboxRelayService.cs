using System.Text.Json;
using OrderService.Data;
using OrderService.Data.Entities;
using OrderService.Services;
using Shared.Events;
using Shared.Outbox;

namespace OrderService.Services.Background;

/// <summary>Outbox relay；掃描與推送邏輯見 <see cref="OutboxRelayServiceBase{TDbContext, TMessage}"/>。</summary>
public class OutboxRelayService(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxRelayService> logger) : OutboxRelayServiceBase<OrderDbContext, OutboxMessage>(scopeFactory, logger)
{
    /// <inheritdoc/>
    protected override object? DeserializeEvent(OutboxMessage message) => message.EventType switch
    {
        var t when t.StartsWith("audit.", StringComparison.Ordinal)
            => JsonSerializer.Deserialize<AuditLogRequestedEvent>(message.Payload, OutboxJson.Options),
        OrderEventPublisher.OrderCompletedType
            => JsonSerializer.Deserialize<OrderCompletedEvent>(message.Payload, OutboxJson.Options),
        var t when t.StartsWith("email.", StringComparison.Ordinal)
            => JsonSerializer.Deserialize<EmailRequestedEvent>(message.Payload, OutboxJson.Options),
        _ => null,
    };
}
