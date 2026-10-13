namespace Accounts.Domain;

public enum AccountOperationType
{
    Debit,
    Credit
}

public class AccountOperation
{
    public Guid Id { get; private set; }
    public Guid AccountId { get; private set; }
    public string OperationId { get; private set; }
    public AccountOperationType Type { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private AccountOperation() // EF Core
    {
        OperationId = null!;
    }

    internal AccountOperation(Guid accountId, string operationId, AccountOperationType type, decimal amount)
    {
        if (string.IsNullOrWhiteSpace(operationId))
            throw new ArgumentException("OperationId is required.", nameof(operationId));

        Id = Guid.NewGuid();
        AccountId = accountId;
        OperationId = operationId;
        Type = type;
        Amount = amount;
        CreatedAtUtc = DateTime.UtcNow;
    }
}