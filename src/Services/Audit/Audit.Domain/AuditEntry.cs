namespace Audit.Domain;

public class AuditEntry
{
    public Guid Id { get; private set; }
    public string MessageId { get; private set; }
    public string EventType { get; private set; }
    public string Payload { get; private set; }
    public DateTime ReceivedAtUtc { get; private set; }

    private AuditEntry() { } // EF Core

    public AuditEntry(string messageId, string eventType, string payload)
    {
        if (string.IsNullOrWhiteSpace(messageId))
            throw new ArgumentException("MessageId is required.", nameof(messageId));

        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("EventType is required.", nameof(eventType));

        if (string.IsNullOrWhiteSpace(payload))
            throw new ArgumentException("Payload is required.", nameof(payload));

        Id = Guid.NewGuid();
        MessageId = messageId;
        EventType = eventType;
        Payload = payload;
        ReceivedAtUtc = DateTime.UtcNow;
    }
}