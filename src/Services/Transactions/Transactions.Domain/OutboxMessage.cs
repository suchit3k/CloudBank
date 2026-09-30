namespace Transactions.Domain;

public class OutboxMessage
{
    public Guid Id { get; private set; }
    public string EventType { get; private set; }
    public string Payload { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? PublishedAtUtc { get; private set; }
    public int AttemptCount { get; private set; }
    public string? LastError { get; private set; }

    private OutboxMessage() { } // EF Core

    public OutboxMessage(string eventType, string payload)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("EventType is required.", nameof(eventType));

        if (string.IsNullOrWhiteSpace(payload))
            throw new ArgumentException("Payload is required.", nameof(payload));

        Id = Guid.NewGuid();
        EventType = eventType;
        Payload = payload;
        CreatedAtUtc = DateTime.UtcNow;
        AttemptCount = 0;
    }

    public bool IsPublished => PublishedAtUtc.HasValue;

    public void MarkAsPublished()
    {
        PublishedAtUtc = DateTime.UtcNow;
    }

    public void RecordFailedAttempt(string error)
    {
        AttemptCount++;
        LastError = error;
    }
}