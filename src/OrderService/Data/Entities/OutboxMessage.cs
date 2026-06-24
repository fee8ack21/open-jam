using Shared.Audit;

namespace OrderService.Data.Entities;

public class OutboxMessage : ICreatedAt
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string EventType { get; set; } = "";
    public string Payload { get; set; } = "";
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; set; }
}
