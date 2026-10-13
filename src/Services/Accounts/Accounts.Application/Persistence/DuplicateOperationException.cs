namespace Accounts.Application.Persistence;

/// <summary>Thrown when saving an operation whose OperationId is already recorded.</summary>
public class DuplicateOperationException : Exception
{
    public DuplicateOperationException(Exception innerException)
        : base("This operation has already been recorded.", innerException)
    {
    }
}