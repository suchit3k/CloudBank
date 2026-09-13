using MediatR;

namespace Transactions.Application.Transactions.Queries.GetTransactionById;

public record GetTransactionByIdQuery(Guid TransactionId) : IRequest<TransactionDto>;