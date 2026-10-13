using Accounts.Domain;

namespace Accounts.Application.Accounts.Commands;

internal static class OperationGuard
{
    /// <summary>
    /// An OperationId was already recorded. Confirms the earlier operation matches this request;
    /// throws if the OperationId was reused for something different.
    /// </summary>
    public static void EnsureSameOperation(
        AccountOperation existing, Guid accountId, AccountOperationType type, decimal amount)
    {
        bool same = existing.AccountId == accountId
                    && existing.Type == type
                    && existing.Amount == amount;

        if (!same)
            throw new OperationConflictException(existing.OperationId);
    }
}