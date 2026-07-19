using System.Text.Json;
using Auth.Data;
using Auth.Data.Entities;
using Shared.Events;
using Shared.Outbox;

namespace Auth.Services.Background;

/// <summary>Outbox relay；掃描與推送邏輯見 <see cref="OutboxRelayServiceBase{TDbContext, TMessage}"/>。</summary>
public class OutboxRelayService(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxRelayService> logger) : OutboxRelayServiceBase<AppDbContext, OutboxMessage>(scopeFactory, logger)
{
    /// <inheritdoc/>
    protected override object? DeserializeEvent(OutboxMessage message) => message.EventType switch
    {
        var t when t.StartsWith("email.", StringComparison.Ordinal)
            => JsonSerializer.Deserialize<EmailRequestedEvent>(message.Payload),
        "user.registered" => JsonSerializer.Deserialize<UserRegisteredEvent>(message.Payload),
        _ => null,
    };
}
