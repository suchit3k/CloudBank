using Accounts.Application.Persistence;
using Accounts.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Accounts.Infrastructure.Persistence;

public class AccountRepository : IAccountRepository
{
    private readonly AccountsDbContext _dbContext;

    public AccountRepository(AccountsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Account account, CancellationToken cancellationToken)
    {
        await _dbContext.Accounts.AddAsync(account, cancellationToken);
    }

    public async Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public Task<AccountOperation?> GetOperationAsync(string operationId, CancellationToken cancellationToken)
    {
        return _dbContext.AccountOperations
            .FirstOrDefaultAsync(o => o.OperationId == operationId, cancellationToken);
    }

    public async Task AddOperationAsync(AccountOperation operation, CancellationToken cancellationToken)
    {
        await _dbContext.AccountOperations.AddAsync(operation, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "IX_account_operations_OperationId"
        })
        {
            throw new DuplicateOperationException(ex);
        }
    }
}