using System.Text.Json;
using Shared.Events;
using Shared.Outbox;
using StoreService.Data;
using StoreService.Data.Entities;

namespace StoreService.Services.Background;

/// <summary>Outbox relay；掃描與推送邏輯見 <see cref="OutboxRelayServiceBase{TDbContext, TMessage}"/>。</summary>
public class OutboxRelayService(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxRelayService> logger) : OutboxRelayServiceBase<StoreDbContext, OutboxMessage>(scopeFactory, logger)
{
    /// <inheritdoc/>
    protected override object? DeserializeEvent(OutboxMessage message) => message.EventType switch
    {
        var t when t.StartsWith("email.", StringComparison.Ordinal)
            => JsonSerializer.Deserialize<EmailRequestedEvent>(message.Payload),
        StoreEventPublisher.StoreFollowerChangedType
            => JsonSerializer.Deserialize<StoreFollowerChangedEvent>(message.Payload),
        StoreEventPublisher.StoreProvisionedType
            => JsonSerializer.Deserialize<StoreProvisionedEvent>(message.Payload),
        _ => JsonSerializer.Deserialize<AuditLogRequestedEvent>(message.Payload),
    };
}
