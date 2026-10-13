using Accounts.Domain;

namespace Accounts.Application.Persistence;

public interface IAccountRepository
{
    Task AddAsync(Account account, CancellationToken cancellationToken);
    Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<AccountOperation?> GetOperationAsync(string operationId, CancellationToken cancellationToken);
    Task AddOperationAsync(AccountOperation operation, CancellationToken cancellationToken);

    /// <exception cref="DuplicateOperationException">An OperationId being saved already exists.</exception>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}