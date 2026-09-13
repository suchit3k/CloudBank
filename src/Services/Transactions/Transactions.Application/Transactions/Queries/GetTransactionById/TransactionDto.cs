namespace Transactions.Application.Transactions.Queries.GetTransactionById;

public record TransactionDto(
    Guid Id,
    Guid SenderAccountId,
    Guid ReceiverAccountId,
    decimal Amount,
    string Currency,
    string Status,
    string? FailureReason,
    DateTime CreatedAt,
    DateTime? CompletedAt);