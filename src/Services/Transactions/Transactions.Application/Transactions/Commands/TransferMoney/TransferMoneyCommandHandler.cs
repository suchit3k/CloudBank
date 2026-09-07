using Transactions.Application.Persistence;
using Transactions.Application.Services;
using Transactions.Domain;
using MediatR;

namespace Transactions.Application.Transactions.Commands.TransferMoney;

public class TransferMoneyCommandHandler : IRequestHandler<TransferMoneyCommand, Guid>
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IAccountsServiceClient _accountsServiceClient;

    public TransferMoneyCommandHandler(
        ITransactionRepository transactionRepository,
        IAccountsServiceClient accountsServiceClient)
    {
        _transactionRepository = transactionRepository;
        _accountsServiceClient = accountsServiceClient;
    }

    public async Task<Guid> Handle(TransferMoneyCommand request, CancellationToken cancellationToken)
    {
        // Step 1: Create Pending transaction record
        var transaction = new Transaction(
            request.SenderAccountId,
            request.ReceiverAccountId,
            request.Amount,
            request.Currency,
            request.IdempotencyKey);

        await _transactionRepository.AddAsync(transaction, cancellationToken);
        await _transactionRepository.SaveChangesAsync(cancellationToken);

        // Step 2: Mark Processing
        transaction.MarkAsProcessing();
        await _transactionRepository.SaveChangesAsync(cancellationToken);

        // Step 3: Withdraw from sender
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

        return transaction.Id;
    }
}