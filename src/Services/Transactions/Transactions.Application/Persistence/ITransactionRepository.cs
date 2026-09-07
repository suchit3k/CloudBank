namespace Transactions.Application.Persistence;

public interface ITransactionRepository
{
    Task AddAsync(Domain.Transaction transaction, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}