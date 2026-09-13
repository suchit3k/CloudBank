using Accounts.Application.Persistence;
using MediatR;

namespace Accounts.Application.Accounts.Commands.FreezeAccount;

public class FreezeAccountCommandHandler : IRequestHandler<FreezeAccountCommand>
{
    private readonly IAccountRepository _accountRepository;

    public FreezeAccountCommandHandler(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task Handle(FreezeAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken);

        if (account is null)
            throw new KeyNotFoundException($"Account with id {request.AccountId} was not found.");

        account.Freeze();

        await _accountRepository.SaveChangesAsync(cancellationToken);
    }
}