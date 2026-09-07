using Transactions.Application.Persistence;
using Transactions.Domain;

namespace Transactions.Infrastructure.Persistence;

public class TransactionRepository : ITransactionRepository
{
    private readonly TransactionDbContext _dbContext;

    public TransactionRepository(TransactionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Transaction transaction, CancellationToken cancellationToken)
    {
        await _dbContext.Transactions.AddAsync(transaction, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}