using FluentValidation;

namespace Accounts.Application.Accounts.Commands.Deposit;

public class DepositCommandValidator : AbstractValidator<DepositCommand>
{
    public DepositCommandValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.OperationId)
            .NotEmpty()
            .MaximumLength(100); // must match the account_operations.OperationId column
    }
}