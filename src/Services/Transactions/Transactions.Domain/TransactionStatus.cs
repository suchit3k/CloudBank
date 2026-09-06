namespace Transactions.Domain;

public enum TransactionStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    Reversed
}