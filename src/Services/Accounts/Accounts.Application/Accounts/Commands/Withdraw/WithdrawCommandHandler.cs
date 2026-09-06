using Accounts.Application.Persistence;
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
        var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken);

        if (account is null)
            throw new KeyNotFoundException($"Account with id {request.AccountId} was not found.");

        account.Withdraw(request.Amount); // throws InvalidOperationException if insufficient funds

        await _accountRepository.SaveChangesAsync(cancellationToken);
    }
}