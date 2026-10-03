namespace Transactions.Application.Services;

public interface IEventPublisher
{
    Task PublishAsync(string messageId, string eventType, string payload, CancellationToken cancellationToken);
}