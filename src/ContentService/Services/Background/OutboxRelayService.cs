using System.Text.Json;
using ContentService.Data;
using ContentService.Data.Entities;
using Shared.Events;
using Shared.Outbox;

namespace ContentService.Services.Background;

/// <summary>Outbox relay；掃描與推送邏輯見 <see cref="OutboxRelayServiceBase{TDbContext, TMessage}"/>。本服務僅發布 <see cref="AuditLogRequestedEvent"/>。</summary>
public class OutboxRelayService(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxRelayService> logger) : OutboxRelayServiceBase<ContentDbContext, OutboxMessage>(scopeFactory, logger)
{
    /// <inheritdoc/>
    protected override object? DeserializeEvent(OutboxMessage message) =>
        JsonSerializer.Deserialize<AuditLogRequestedEvent>(message.Payload);
}
