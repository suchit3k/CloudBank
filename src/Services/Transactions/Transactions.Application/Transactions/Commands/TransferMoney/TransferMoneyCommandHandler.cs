using MediatR;
using Transactions.Application.Events;
using Transactions.Application.Persistence;
using Transactions.Application.Services;
using Transactions.Domain;

namespace Transactions.Application.Transactions.Commands.TransferMoney;

public class TransferMoneyCommandHandler : IRequestHandler<TransferMoneyCommand, Guid>
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IAccountsServiceClient _accountsServiceClient;
    private readonly IEventPublisher _eventPublisher;

    public TransferMoneyCommandHandler(
        ITransactionRepository transactionRepository,
        IAccountsServiceClient accountsServiceClient,
        IEventPublisher eventPublisher)
    {
        _transactionRepository = transactionRepository;
        _accountsServiceClient = accountsServiceClient;
        _eventPublisher = eventPublisher;
    }

    public async Task<Guid> Handle(TransferMoneyCommand request, CancellationToken cancellationToken)
    {

        // Step 0: Idempotency check
        var existingTransaction = await _transactionRepository.GetByIdempotencyKeyAsync(request.IdempotencyKey, cancellationToken);
        if (existingTransaction is not null)
        {
            return existingTransaction.Id;
        }

        // Step 1: Create Pending transaction record
        var transaction = new Transaction(
            request.SenderAccountId,
            request.ReceiverAccountId,
            request.Amount,
            request.Currency,
            request.IdempotencyKey);

        await _transactionRepository.AddAsync(transaction, cancellationToken);
        await _transactionRepository.SaveChangesAsync(cancellationToken);


        // Step 2: Withdraw from sender
        try
        {
            await _accountsServiceClient.WithdrawAsync(request.SenderAccountId, request.Amount, cancellationToken);
        }
        catch (Exception ex)
        {
            transaction.MarkAsFailed($"Withdrawal failed: {ex.Message}");
            await _transactionRepository.SaveChangesAsync(cancellationToken);
            return transaction.Id;
        }
        // Step 3: Mark Processing
        transaction.MarkAsProcessing();
        await _transactionRepository.SaveChangesAsync(cancellationToken);

        // Step 4: Deposit to receiver
        try
        {
            await _accountsServiceClient.DepositAsync(request.ReceiverAccountId, request.Amount, cancellationToken);
        }
        catch (Exception ex)
        {
            // Compensating action: give the money back to the sender
            await _accountsServiceClient.DepositAsync(request.SenderAccountId, request.Amount, cancellationToken);

            transaction.MarkAsReversed($"Deposit to receiver failed: {ex.Message}");
            await _transactionRepository.SaveChangesAsync(cancellationToken);
            return transaction.Id;
        }

        // Both steps succeeded
        transaction.MarkAsCompleted();
        await _transactionRepository.SaveChangesAsync(cancellationToken);

        // Publish event AFTER the transaction is durably marked Completed
        try
        {
            await _eventPublisher.PublishTransactionCompletedAsync(
                new TransactionCompletedEvent(
                    transaction.Id,
                    transaction.SenderAccountId,
                    transaction.ReceiverAccountId,
                    transaction.Amount,
                    transaction.Currency,
                    transaction.CompletedAt!.Value),
                cancellationToken);
        }
        catch (Exception ex)
        {
            // Deliberately NOT re-thrown — explained below
            Console.WriteLine($"Failed to publish TransactionCompletedEvent for {transaction.Id}: {ex.Message}");
        }

        return transaction.Id;
    }
}