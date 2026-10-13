namespace Accounts.Application.Accounts.Commands;

/// <summary>Thrown when an OperationId is reused for a different operation.</summary>
public class OperationConflictException : Exception
{
    public OperationConflictException(string operationId)
        : base($"OperationId '{operationId}' was already used for a different operation.")
    {
    }
}