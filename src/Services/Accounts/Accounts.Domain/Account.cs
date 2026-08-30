namespace Accounts.Domain;

public class Account
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string AccountType { get; private set; } // "Checking" or "Savings"
    public decimal Balance { get; private set; }
    public string Currency { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsFrozen { get; private set; }

    private Account() { } // Required by EF Core, kept private so it's not misused elsewhere

    public Account(Guid userId, string accountType, string currency = "USD")
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));

        if (string.IsNullOrWhiteSpace(accountType))
            throw new ArgumentException("Account type is required.", nameof(accountType));

        Id = Guid.NewGuid();
        UserId = userId;
        AccountType = accountType;
        Balance = 0m;
        Currency = currency;
        CreatedAt = DateTime.UtcNow;
        IsFrozen = false;
    }

    public void Deposit(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Deposit amount must be positive.");
        if (IsFrozen)
            throw new InvalidOperationException("Cannot deposit into a frozen account.");

        Balance += amount;
    }

    public void Withdraw(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Withdrawal amount must be positive.");
        if (IsFrozen)
            throw new InvalidOperationException("Cannot withdraw from a frozen account.");
        if (Balance < amount)
            throw new InvalidOperationException("Insufficient funds.");

        Balance -= amount;
    }

    public void Freeze() => IsFrozen = true;
    public void Unfreeze() => IsFrozen = false;
}