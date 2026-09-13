using Transactions.Application.Persistence;
using MediatR;

namespace Transactions.Application.Transactions.Queries.GetTransactionById;

public class GetTransactionByIdQueryHandler : IRequestHandler<GetTransactionByIdQuery, TransactionDto>
{
    private readonly ITransactionRepository _transactionRepository;

    public GetTransactionByIdQueryHandler(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task<TransactionDto> Handle(GetTransactionByIdQuery request, CancellationToken cancellationToken)
    {
        var transaction = await _transactionRepository.GetByIdAsync(request.TransactionId, cancellationToken);

        if (transaction is null)
            throw new KeyNotFoundException($"Transaction with id {request.TransactionId} was not found.");

        return new TransactionDto(
            transaction.Id,
            transaction.SenderAccountId,
            transaction.ReceiverAccountId,
            transaction.Amount,
            transaction.Currency,
            transaction.Status.ToString(),
            transaction.FailureReason,
            transaction.CreatedAt,
            transaction.CompletedAt);
    }
}