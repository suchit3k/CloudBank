using MediatR;

namespace Accounts.Application.Accounts.Commands.CreateAccount;

public record CreateAccountCommand(Guid UserId, string AccountType, string Currency = "USD")
    : IRequest<Guid>;