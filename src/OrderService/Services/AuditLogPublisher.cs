using System.Text.Json;
using OrderService.Data;
using OrderService.Data.Entities;
using Shared.Events;

namespace OrderService.Services;

public class AuditLogPublisher(OrderDbContext db)
{
    public void Add(Guid? who, string action, string target, Guid targetId, Guid? tenant)
    {
        var outbox = new OutboxMessage { EventType = "audit." + action };
        outbox.Payload = JsonSerializer.Serialize(new AuditLogRequestedEvent(
            OutboxMessageId: outbox.Id,
            Who: who,
            Action: action,
            Target: target,
            TargetId: targetId,
            Result: "success",
            Before: null,
            After: null,
            Ip: null,
            UserAgent: null,
            Tenant: tenant,
            OccurredAt: DateTimeOffset.UtcNow,
            CorrelationId: null
        ));
        db.OutboxMessages.Add(outbox);
    }
}
