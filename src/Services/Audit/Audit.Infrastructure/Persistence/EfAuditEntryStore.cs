using Audit.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Audit.Infrastructure.Persistence;

public class EfAuditEntryStore : IAuditEntryStore
{
    private readonly AuditDbContext _dbContext;

    public EfAuditEntryStore(AuditDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsAsync(string messageId, CancellationToken cancellationToken)
    {
        return _dbContext.AuditEntries.AnyAsync(a => a.MessageId == messageId, cancellationToken);
    }

    public async Task<bool> TryAddAsync(AuditEntry entry, CancellationToken cancellationToken)
    {
        _dbContext.AuditEntries.Add(entry);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Another copy was stored between the existence check and this insert
            return false;
        }
    }
}