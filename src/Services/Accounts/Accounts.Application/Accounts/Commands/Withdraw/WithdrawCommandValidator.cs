using FluentValidation;

namespace Accounts.Application.Accounts.Commands.Withdraw;

public class WithdrawCommandValidator : AbstractValidator<WithdrawCommand>
{
    public WithdrawCommandValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.OperationId)
            .NotEmpty()
            .MaximumLength(100);
    }
}