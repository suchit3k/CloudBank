using MediatR;

namespace Accounts.Application.Accounts.Commands.FreezeAccount;

public record FreezeAccountCommand(Guid AccountId) : IRequest;