using Accounts.Application.Accounts.Commands.CreateAccount;
using Xunit;

namespace Accounts.Application.Tests.Accounts.Commands.CreateAccount;

public class CreateAccountCommandValidatorTests
{
    private readonly CreateAccountCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        // Arrange
        var command = new CreateAccountCommand(Guid.NewGuid(), "Checking", "USD");

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("Business")]
    [InlineData("")]
    [InlineData("checking")] // lowercase — should fail since it's not an exact match
    public void Validate_InvalidAccountType_HasError(string invalidType)
    {
        // Arrange
        var command = new CreateAccountCommand(Guid.NewGuid(), invalidType, "USD");

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAccountCommand.AccountType));
    }

    [Fact]
    public void Validate_EmptyUserId_HasError()
    {
        // Arrange
        var command = new CreateAccountCommand(Guid.Empty, "Checking", "USD");

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAccountCommand.UserId));
    }
}