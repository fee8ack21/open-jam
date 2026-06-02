namespace Shared.Events;

public record EmailRequestedEvent(
    Guid   OutboxMessageId,
    string To,
    string Subject,
    string BodyHtml,
    string EventType
);
