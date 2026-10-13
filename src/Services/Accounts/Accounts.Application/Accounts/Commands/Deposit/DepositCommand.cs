using MediatR;

namespace Accounts.Application.Accounts.Commands.Deposit;

public record DepositCommand(Guid AccountId, decimal Amount, string OperationId) : IRequest;