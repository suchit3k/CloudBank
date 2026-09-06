using Accounts.Application.Accounts.Queries.GetAccountById;
using Accounts.Application.Persistence;
using Accounts.Domain;
using NSubstitute;
using Xunit;

namespace Accounts.Application.Tests.Accounts.Queries.GetAccountById;

public class GetAccountByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ExistingAccount_ReturnsDto()
    {
        // Arrange
        var account = new Account(Guid.NewGuid(), "Checking", "USD");
        var mockRepository = Substitute.For<IAccountRepository>();
        mockRepository
            .GetByIdAsync(account.Id, Arg.Any<CancellationToken>())
            .Returns(account);

        var handler = new GetAccountByIdQueryHandler(mockRepository);
        var query = new GetAccountByIdQuery(account.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(account.Id, result.Id);
        Assert.Equal(account.UserId, result.UserId);
        Assert.Equal(account.AccountType, result.AccountType);
    }

    [Fact]
    public async Task Handle_NonExistentAccount_ThrowsKeyNotFoundException()
    {
        // Arrange
        var mockRepository = Substitute.For<IAccountRepository>();
        mockRepository
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Account?)null);

        var handler = new GetAccountByIdQueryHandler(mockRepository);
        var query = new GetAccountByIdQuery(Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => handler.Handle(query, CancellationToken.None));
    }
}