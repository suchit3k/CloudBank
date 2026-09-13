namespace Transactions.Application.Persistence;

public interface ITransactionRepository
{
    Task AddAsync(Domain.Transaction transaction, CancellationToken cancellationToken);
    Task<Domain.Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}