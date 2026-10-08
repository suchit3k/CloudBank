using Audit.Domain;

namespace Audit.Infrastructure.Persistence;

public interface IAuditEntryStore
{
    Task<bool> ExistsAsync(string messageId, CancellationToken cancellationToken);

    /// <summary>Returns false if an entry with the same MessageId already exists.</summary>
    Task<bool> TryAddAsync(AuditEntry entry, CancellationToken cancellationToken);
}