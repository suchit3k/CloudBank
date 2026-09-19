using Transactions.Application.Events;

namespace Transactions.Application.Services;

public interface IEventPublisher
{
    Task PublishTransactionCompletedAsync(TransactionCompletedEvent @event, CancellationToken cancellationToken);
}