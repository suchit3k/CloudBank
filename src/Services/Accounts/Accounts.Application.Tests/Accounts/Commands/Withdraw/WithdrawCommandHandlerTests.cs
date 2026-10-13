using Accounts.Application.Accounts.Commands.Withdraw;
using Accounts.Application.Persistence;
using Accounts.Domain;
using NSubstitute;
using Xunit;

namespace Accounts.Application.Tests.Accounts.Commands.Withdraw;

public class WithdrawCommandHandlerTests
{
    private readonly IAccountRepository _repository = Substitute.For<IAccountRepository>();
    private readonly WithdrawCommandHandler _handler;

    public WithdrawCommandHandlerTests()
    {
        _handler = new WithdrawCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_NewOperation_AppliesWithdrawalAndRecordsDebit()
    {
        var account = new Account(Guid.NewGuid(), "Checking");
        account.Deposit(200m, "seed");
        _repository.GetOperationAsync("op-1", Arg.Any<CancellationToken>()).Returns((AccountOperation?)null);
        _repository.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);

        await _handler.Handle(new WithdrawCommand(account.Id, 50m, "op-1"), CancellationToken.None);

        Assert.Equal(150m, account.Balance);
        await _repository.Received(1).AddOperationAsync(
            Arg.Is<AccountOperation>(o => o.Type == AccountOperationType.Debit && o.Amount == 50m),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InsufficientFunds_RejectsAndRecordsNothing()
    {
        var account = new Account(Guid.NewGuid(), "Checking"); // balance 0
        _repository.GetOperationAsync("op-1", Arg.Any<CancellationToken>()).Returns((AccountOperation?)null);
        _repository.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new WithdrawCommand(account.Id, 50m, "op-1"), CancellationToken.None));

        await _repository.DidNotReceive().AddOperationAsync(Arg.Any<AccountOperation>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}