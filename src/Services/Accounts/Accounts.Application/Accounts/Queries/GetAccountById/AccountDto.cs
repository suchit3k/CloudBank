namespace Accounts.Application.Accounts.Queries.GetAccountById;

public record AccountDto(
    Guid Id,
    Guid UserId,
    string AccountType,
    decimal Balance,
    string Currency,
    DateTime CreatedAt,
    bool IsFrozen);