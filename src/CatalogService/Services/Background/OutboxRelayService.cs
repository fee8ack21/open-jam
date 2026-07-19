using System.Text.Json;
using CatalogService.Data;
using CatalogService.Data.Entities;
using Shared.Events;
using Shared.Outbox;

namespace CatalogService.Services.Background;

/// <summary>Outbox relay；掃描與推送邏輯見 <see cref="OutboxRelayServiceBase{TDbContext, TMessage}"/>。</summary>
public class OutboxRelayService(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxRelayService> logger) : OutboxRelayServiceBase<CatalogDbContext, OutboxMessage>(scopeFactory, logger)
{
    /// <inheritdoc/>
    protected override object? DeserializeEvent(OutboxMessage message) => message.EventType switch
    {
        CatalogEventPublisher.CatalogPublishedType
            => JsonSerializer.Deserialize<CatalogPublishedEvent>(message.Payload),
        CatalogEventPublisher.CatalogVersionReleasedType
            => JsonSerializer.Deserialize<CatalogVersionReleasedEvent>(message.Payload),
        _ => JsonSerializer.Deserialize<AuditLogRequestedEvent>(message.Payload),
    };
}
