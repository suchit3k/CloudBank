using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Transactions.Application.Events;
using Transactions.Application.Services;

namespace Transactions.Infrastructure.Services;

public class ServiceBusEventPublisher : IEventPublisher, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSender _sender;
    private const string TopicName = "transaction-events";

    public ServiceBusEventPublisher(string connectionString)
    {
        _client = new ServiceBusClient(connectionString);
        _sender = _client.CreateSender(TopicName);
    }

    public async Task PublishTransactionCompletedAsync(TransactionCompletedEvent @event, CancellationToken cancellationToken)
    {
        string json = JsonSerializer.Serialize(@event);

        var message = new ServiceBusMessage(json)
        {
            ContentType = "application/json",
            Subject = nameof(TransactionCompletedEvent)
        };

        await _sender.SendMessageAsync(message, cancellationToken);
    }

    public async Task PublishAsync(string messageId, string eventType, string payload, CancellationToken cancellationToken)
    {
        var message = new ServiceBusMessage(payload)
        {
            MessageId = messageId,
            ContentType = "application/json",
            Subject = eventType
        };

        await _sender.SendMessageAsync(message, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _sender.DisposeAsync();
        await _client.DisposeAsync();
    }
}