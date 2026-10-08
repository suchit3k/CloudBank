using System.Text.Json;
using Audit.Domain;
using Audit.Infrastructure.Persistence;

namespace Audit.Infrastructure.Messaging;

public enum AuditHandlingOutcome
{
    Stored,
    Duplicate,
    InvalidMessage
}

public record AuditHandlingResult(AuditHandlingOutcome Outcome, string? Reason = null, string? Description = null)
{
    public static AuditHandlingResult Stored() => new(AuditHandlingOutcome.Stored);
    public static AuditHandlingResult Duplicate() => new(AuditHandlingOutcome.Duplicate);
    public static AuditHandlingResult Invalid(string reason, string description) =>
        new(AuditHandlingOutcome.InvalidMessage, reason, description);
}

public class AuditMessageHandler
{
    private readonly IAuditEntryStore _store;

    public AuditMessageHandler(IAuditEntryStore store)
    {
        _store = store;
    }

    public async Task<AuditHandlingResult> HandleAsync(
        string? messageId, string? eventType, string payload, CancellationToken cancellationToken)
    {
        // Permanent failures: will never succeed, so report them as invalid
        if (string.IsNullOrWhiteSpace(messageId) || string.IsNullOrWhiteSpace(eventType))
            return AuditHandlingResult.Invalid("MissingMetadata", "Message has no MessageId or Subject.");

        try
        {
            JsonDocument.Parse(payload).Dispose();
        }
        catch (JsonException ex)
        {
            return AuditHandlingResult.Invalid("InvalidPayload", ex.Message);
        }

        // Idempotency
        if (await _store.ExistsAsync(messageId, cancellationToken))
            return AuditHandlingResult.Duplicate();

        bool added = await _store.TryAddAsync(new AuditEntry(messageId, eventType, payload), cancellationToken);
        return added ? AuditHandlingResult.Stored() : AuditHandlingResult.Duplicate();

        // Transient failures (e.g. database down) are NOT caught here.
        // They propagate as exceptions, and the consumer retries with backoff.
    }
}