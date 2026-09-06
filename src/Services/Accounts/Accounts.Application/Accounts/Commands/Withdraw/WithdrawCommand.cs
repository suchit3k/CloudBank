using MediatR;

namespace Accounts.Application.Accounts.Commands.Withdraw;

public record WithdrawCommand(Guid AccountId, decimal Amount) : IRequest;