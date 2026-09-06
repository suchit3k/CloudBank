using Accounts.Application.Persistence;
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
        var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken);

        if (account is null)
            throw new KeyNotFoundException($"Account with id {request.AccountId} was not found.");

        account.Deposit(request.Amount);

        await _accountRepository.SaveChangesAsync(cancellationToken);
    }
}