using Accounts.Application.Persistence;
using MediatR;

namespace Accounts.Application.Accounts.Queries.GetAccountById;

public class GetAccountByIdQueryHandler : IRequestHandler<GetAccountByIdQuery, AccountDto>
{
    private readonly IAccountRepository _accountRepository;

    public GetAccountByIdQueryHandler(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task<AccountDto> Handle(GetAccountByIdQuery request, CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken);

        if (account is null)
            throw new KeyNotFoundException($"Account with id {request.AccountId} was not found.");

        return new AccountDto(
            account.Id,
            account.UserId,
            account.AccountType,
            account.Balance,
            account.Currency,
            account.CreatedAt,
            account.IsFrozen);
    }
}