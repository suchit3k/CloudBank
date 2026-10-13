using Accounts.Application.Accounts.Commands;
using Accounts.Application.Accounts.Commands.Deposit;
using Accounts.Application.Persistence;
using Accounts.Domain;
using NSubstitute;
using Xunit;

namespace Accounts.Application.Tests.Accounts.Commands.Deposit;

public class DepositCommandHandlerTests
{
    private readonly IAccountRepository _repository = Substitute.For<IAccountRepository>();
    private readonly DepositCommandHandler _handler;

    public DepositCommandHandlerTests()
    {
        _handler = new DepositCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_NewOperation_AppliesDepositAndRecordsIt()
    {
        var account = new Account(Guid.NewGuid(), "Checking");
        _repository.GetOperationAsync("op-1", Arg.Any<CancellationToken>()).Returns((AccountOperation?)null);
        _repository.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);

        await _handler.Handle(new DepositCommand(account.Id, 100m, "op-1"), CancellationToken.None);

        Assert.Equal(100m, account.Balance);
        await _repository.Received(1).AddOperationAsync(
            Arg.Is<AccountOperation>(o =>
                o.OperationId == "op-1" &&
                o.AccountId == account.Id &&
                o.Type == AccountOperationType.Credit &&
                o.Amount == 100m),
            Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SameOperationAlreadyApplied_ChangesNothing()
    {
        var account = new Account(Guid.NewGuid(), "Checking");
        var existing = account.Deposit(100m, "op-1"); // balance is now 100: the first application
        _repository.GetOperationAsync("op-1", Arg.Any<CancellationToken>()).Returns(existing);

        await _handler.Handle(new DepositCommand(account.Id, 100m, "op-1"), CancellationToken.None);

        Assert.Equal(100m, account.Balance); // still 100, not 200
        await _repository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OperationIdReusedWithDifferentAmount_ThrowsConflict()
    {
        var account = new Account(Guid.NewGuid(), "Checking");
        var existing = account.Deposit(100m, "op-1");
        _repository.GetOperationAsync("op-1", Arg.Any<CancellationToken>()).Returns(existing);

        await Assert.ThrowsAsync<OperationConflictException>(
            () => _handler.Handle(new DepositCommand(account.Id, 500m, "op-1"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ConcurrentDuplicateDetectedOnSave_IsTreatedAsSuccess()
    {
        var account = new Account(Guid.NewGuid(), "Checking");
        _repository.GetOperationAsync("op-1", Arg.Any<CancellationToken>()).Returns((AccountOperation?)null);
        _repository.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new DuplicateOperationException(new Exception("unique violation")));

        // Must complete without throwing: the other request already applied it
        await _handler.Handle(new DepositCommand(account.Id, 100m, "op-1"), CancellationToken.None);
    }
}