using Accounts.Domain;

namespace Accounts.Application.Persistence;

public interface IAccountRepository
{
    Task AddAsync(Account account, CancellationToken cancellationToken);
    Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}