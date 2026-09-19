namespace Transactions.Application.Events;

public record TransactionCompletedEvent(
    Guid TransactionId,
    Guid SenderAccountId,
    Guid ReceiverAccountId,
    decimal Amount,
    string Currency,
    DateTime CompletedAtUtc);