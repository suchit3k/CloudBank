namespace Transactions.Application.Services;

public interface IAccountsServiceClient
{
    Task WithdrawAsync(Guid accountId, decimal amount, CancellationToken cancellationToken);
    Task DepositAsync(Guid accountId, decimal amount, CancellationToken cancellationToken);
}