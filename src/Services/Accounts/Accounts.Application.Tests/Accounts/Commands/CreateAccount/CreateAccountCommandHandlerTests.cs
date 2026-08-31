using Accounts.Application.Accounts.Commands.CreateAccount;
using Accounts.Application.Persistence;
using Accounts.Domain;
using NSubstitute;
using Xunit;

namespace Accounts.Application.Tests.Accounts.Commands.CreateAccount;

public class CreateAccountCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidCommand_CreatesAccountAndReturnsId()
    {
        // Arrange
        var mockRepository = Substitute.For<IAccountRepository>();
        var handler = new CreateAccountCommandHandler(mockRepository);

        var command = new CreateAccountCommand(
            UserId: Guid.NewGuid(),
            AccountType: "Checking",
            Currency: "USD");

        // Act
        var resultId = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, resultId);

        await mockRepository.Received(1).AddAsync(
            Arg.Is<Account>(a =>
                a.UserId == command.UserId &&
                a.AccountType == command.AccountType &&
                a.Currency == command.Currency &&
                a.Balance == 0m),
            Arg.Any<CancellationToken>());

        await mockRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmptyUserId_ThrowsArgumentException()
    {
        // Arrange
        var mockRepository = Substitute.For<IAccountRepository>();
        var handler = new CreateAccountCommandHandler(mockRepository);

        var command = new CreateAccountCommand(
            UserId: Guid.Empty,
            AccountType: "Checking",
            Currency: "USD");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => handler.Handle(command, CancellationToken.None));

        await mockRepository.DidNotReceive().AddAsync(
            Arg.Any<Account>(), Arg.Any<CancellationToken>());
    }
}