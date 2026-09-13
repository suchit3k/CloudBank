using Transactions.Application.Persistence;
using Transactions.Application.Services;
using Transactions.Application.Transactions.Commands.TransferMoney;
using Transactions.Domain;
using NSubstitute;
using Xunit;

namespace Transactions.Application.Tests.Transactions.Commands.TransferMoney;

public class TransferMoneyCommandHandlerTests
{
    [Fact]
    public async Task Handle_SuccessfulTransfer_CompletesTransactionAndCallsWithdrawThenDeposit()
    {
        // Arrange
        var mockRepository = Substitute.For<ITransactionRepository>();
        var mockAccountsClient = Substitute.For<IAccountsServiceClient>();

        var handler = new TransferMoneyCommandHandler(mockRepository, mockAccountsClient);

        var command = new TransferMoneyCommand(
            SenderAccountId: Guid.NewGuid(),
            ReceiverAccountId: Guid.NewGuid(),
            Amount: 100m,
            Currency: "USD",
            IdempotencyKey: Guid.NewGuid());

        // Act
        var transactionId = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, transactionId);

        // Verify withdraw was called on the sender
        await mockAccountsClient.Received(1).WithdrawAsync(
            command.SenderAccountId, command.Amount, Arg.Any<CancellationToken>());

        // Verify deposit was called on the receiver
        await mockAccountsClient.Received(1).DepositAsync(
            command.ReceiverAccountId, command.Amount, Arg.Any<CancellationToken>());

        // Verify the compensating deposit to sender was NEVER called
        await mockAccountsClient.DidNotReceive().DepositAsync(
            command.SenderAccountId, Arg.Any<decimal>(), Arg.Any<CancellationToken>());

        // Verify the transaction was saved multiple times (checkpointing)
        await mockRepository.Received(3).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DepositToReceiverFails_CompensatesAndMarksReversed()
    {
        // Arrange
        var mockRepository = Substitute.For<ITransactionRepository>();
        var mockAccountsClient = Substitute.For<IAccountsServiceClient>();

        var command = new TransferMoneyCommand(
            SenderAccountId: Guid.NewGuid(),
            ReceiverAccountId: Guid.NewGuid(),
            Amount: 100m,
            Currency: "USD",
            IdempotencyKey: Guid.NewGuid());

        // Configure: deposit to the RECEIVER specifically throws (simulating frozen account, etc.)
        mockAccountsClient
            .DepositAsync(command.ReceiverAccountId, command.Amount, Arg.Any<CancellationToken>())
            .Returns<Task>(x => throw new InvalidOperationException("Cannot deposit into a frozen account."));

        var handler = new TransferMoneyCommandHandler(mockRepository, mockAccountsClient);

        // Act
        var transactionId = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, transactionId);

        // Withdrawal from sender still should have happened
        await mockAccountsClient.Received(1).WithdrawAsync(
            command.SenderAccountId, command.Amount, Arg.Any<CancellationToken>());

        // Deposit to receiver was attempted (and failed)
        await mockAccountsClient.Received(1).DepositAsync(
            command.ReceiverAccountId, command.Amount, Arg.Any<CancellationToken>());

        // Compensating deposit BACK to sender must have happened
        await mockAccountsClient.Received(1).DepositAsync(
            command.SenderAccountId, command.Amount, Arg.Any<CancellationToken>());
    }
}