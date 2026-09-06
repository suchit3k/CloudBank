namespace Transactions.Domain;

public class Transaction
{
    public Guid Id { get; private set; }
    public Guid SenderAccountId { get; private set; }
    public Guid ReceiverAccountId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; }
    public TransactionStatus Status { get; private set; }
    public string? FailureReason { get; private set; }
    public Guid IdempotencyKey { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private Transaction() { } // EF Core

    public Transaction(Guid senderAccountId, Guid receiverAccountId, decimal amount, string currency, Guid idempotencyKey)
    {
        if (senderAccountId == Guid.Empty)
            throw new ArgumentException("SenderAccountId cannot be empty.", nameof(senderAccountId));

        if (receiverAccountId == Guid.Empty)
            throw new ArgumentException("ReceiverAccountId cannot be empty.", nameof(receiverAccountId));

        if (senderAccountId == receiverAccountId)
            throw new ArgumentException("Cannot transfer to the same account.");

        if (amount <= 0)
            throw new ArgumentException("Amount must be positive.", nameof(amount));

        Id = Guid.NewGuid();
        SenderAccountId = senderAccountId;
        ReceiverAccountId = receiverAccountId;
        Amount = amount;
        Currency = currency;
        Status = TransactionStatus.Pending;
        IdempotencyKey = idempotencyKey;
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkAsProcessing()
    {
        if (Status != TransactionStatus.Pending)
            throw new InvalidOperationException($"Cannot move to Processing from {Status}.");

        Status = TransactionStatus.Processing;
    }

    public void MarkAsCompleted()
    {
        if (Status != TransactionStatus.Processing)
            throw new InvalidOperationException($"Cannot move to Completed from {Status}.");

        Status = TransactionStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string reason)
    {
        if (Status != TransactionStatus.Pending)
            throw new InvalidOperationException($"Cannot move to Failed from {Status}.");

        Status = TransactionStatus.Failed;
        FailureReason = reason;
    }

    public void MarkAsReversed(string reason)
    {
        if (Status != TransactionStatus.Processing)
            throw new InvalidOperationException($"Cannot move to Reversed from {Status}.");

        Status = TransactionStatus.Reversed;
        FailureReason = reason;
    }
}