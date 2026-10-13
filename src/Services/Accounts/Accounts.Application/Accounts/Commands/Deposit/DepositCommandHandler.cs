using Accounts.Application.Persistence;
using Accounts.Domain;
using MediatR;

namespace Accounts.Application.Accounts.Commands.Deposit;

public class DepositCommandHandler : IRequestHandler<DepositCommand>
{
    private readonly IAccountRepository _accountRepository;

    public DepositCommandHandler(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task Handle(DepositCommand request, CancellationToken cancellationToken)
    {
        // 1. Already applied?
        var existing = await _accountRepository.GetOperationAsync(request.OperationId, cancellationToken);
        if (existing is not null)
        {
            OperationGuard.EnsureSameOperation(existing, request.AccountId, AccountOperationType.Credit, request.Amount);
            return;
        }

        // 2. Apply (business rules run inside Account.Deposit)
        var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken)
            ?? throw new KeyNotFoundException($"Account with id {request.AccountId} was not found.");

        var operation = account.Deposit(request.Amount, request.OperationId);

        // 3–4. Balance change + ledger entry, saved together
        await _accountRepository.AddOperationAsync(operation, cancellationToken);

        try
        {
            await _accountRepository.SaveChangesAsync(cancellationToken);
        }
        catch (DuplicateOperationException)
        {
            // 5. A concurrent duplicate saved first. Our whole save was rolled back
            //    (balance included), so the operation was applied exactly once.
        }
    }
}