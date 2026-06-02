namespace Shared.Events;

public record EmailRequestedEvent(
    Guid                       OutboxMessageId,
    string                     To,
    string                     TemplateKey,
    Dictionary<string, string> Params,
    string                     Locale,
    string                     EventType
);
