using FluentValidation;

namespace Accounts.Application.Accounts.Commands.CreateAccount;

public class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
{
    private static readonly string[] AllowedAccountTypes = { "Checking", "Savings" };
    private static readonly string[] AllowedCurrencies = { "USD", "EUR", "GBP", "INR" };

    public CreateAccountCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required.");

        RuleFor(x => x.AccountType)
            .NotEmpty()
            .Must(type => AllowedAccountTypes.Contains(type))
            .WithMessage($"AccountType must be one of: {string.Join(", ", AllowedAccountTypes)}");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3)
            .Must(currency => AllowedCurrencies.Contains(currency))
            .WithMessage($"Currency must be one of: {string.Join(", ", AllowedCurrencies)}");
    }
}