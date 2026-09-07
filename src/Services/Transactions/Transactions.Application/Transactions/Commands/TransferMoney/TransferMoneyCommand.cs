using MediatR;

namespace Transactions.Application.Transactions.Commands.TransferMoney;

public record TransferMoneyCommand(
    Guid SenderAccountId,
    Guid ReceiverAccountId,
    decimal Amount,
    string Currency,
    Guid IdempotencyKey) : IRequest<Guid>;