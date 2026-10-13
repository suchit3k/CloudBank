using Accounts.Application.Persistence;
using Accounts.Domain;
using MediatR;

namespace Accounts.Application.Accounts.Commands.Withdraw;

public class WithdrawCommandHandler : IRequestHandler<WithdrawCommand>
{
    private readonly IAccountRepository _accountRepository;

    public WithdrawCommandHandler(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task Handle(WithdrawCommand request, CancellationToken cancellationToken)
    {
        var existing = await _accountRepository.GetOperationAsync(request.OperationId, cancellationToken);
        if (existing is not null)
        {
            OperationGuard.EnsureSameOperation(existing, request.AccountId, AccountOperationType.Debit, request.Amount);
            return;
        }

        var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken)
            ?? throw new KeyNotFoundException($"Account with id {request.AccountId} was not found.");

        var operation = account.Withdraw(request.Amount, request.OperationId);

        await _accountRepository.AddOperationAsync(operation, cancellationToken);

        try
        {
            await _accountRepository.SaveChangesAsync(cancellationToken);
        }
        catch (DuplicateOperationException)
        {
            // Concurrent duplicate already applied it; ours was rolled back entirely.
        }
    }
}