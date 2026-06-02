namespace Auth.Data.Entities;

public class OutboxMessage
{
    public Guid      Id          { get; set; }
    public string    EventType   { get; set; } = "";
    public string    Payload     { get; set; } = "";
    public DateTime  CreatedAt   { get; set; }
    public DateTime? ProcessedAt { get; set; }
}
