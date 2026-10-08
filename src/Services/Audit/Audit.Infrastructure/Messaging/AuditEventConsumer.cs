using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Audit.Infrastructure.Messaging;

public class AuditEventConsumer : BackgroundService
{
    private const string TopicName = "transaction-events";
    private const string SubscriptionName = "audit-sub";

    private readonly ServiceBusClient _client;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuditEventConsumer> _logger;
    private ServiceBusProcessor? _processor;

    public AuditEventConsumer(
        ServiceBusClient client,
        IServiceScopeFactory scopeFactory,
        ILogger<AuditEventConsumer> logger)
    {
        _client = client;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _processor = _client.CreateProcessor(TopicName, SubscriptionName, new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxConcurrentCalls = 1
        });

        _processor.ProcessMessageAsync += HandleMessageAsync;
        _processor.ProcessErrorAsync += HandleErrorAsync;

        await _processor.StartProcessingAsync(stoppingToken);
        _logger.LogInformation("Audit consumer started, listening on {Topic}/{Subscription}.", TopicName, SubscriptionName);

        try
        {
            // Keep this background service alive until the app shuts down
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }

        await _processor.StopProcessingAsync();
        await _processor.DisposeAsync();
        _logger.LogInformation("Audit consumer stopped.");
    }

    private async Task HandleMessageAsync(ProcessMessageEventArgs args)
    {
        var message = args.Message;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<AuditMessageHandler>();

            var result = await handler.HandleAsync(
                message.MessageId, message.Subject, message.Body.ToString(), args.CancellationToken);

            switch (result.Outcome)
            {
                case AuditHandlingOutcome.Stored:
                    _logger.LogInformation("Stored audit entry for {EventType} (MessageId {MessageId}).",
                        message.Subject, message.MessageId);
                    await args.CompleteMessageAsync(message, args.CancellationToken);
                    break;

                case AuditHandlingOutcome.Duplicate:
                    _logger.LogInformation("Duplicate message {MessageId} ignored.", message.MessageId);
                    await args.CompleteMessageAsync(message, args.CancellationToken);
                    break;

                case AuditHandlingOutcome.InvalidMessage:
                    _logger.LogWarning("Dead-lettering message {MessageId}: {Reason}.", message.MessageId, result.Reason);
                    await args.DeadLetterMessageAsync(message, result.Reason!, result.Description, args.CancellationToken);
                    break;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            TimeSpan delay = CalculateBackoff(message.DeliveryCount);

            _logger.LogError(ex,
                "Failed to process message {MessageId} (delivery {DeliveryCount}); retrying in {DelaySeconds}s.",
                message.MessageId, message.DeliveryCount, delay.TotalSeconds);

            await Task.Delay(delay, args.CancellationToken);
            await args.AbandonMessageAsync(message, cancellationToken: args.CancellationToken);
        }
    }

    private Task HandleErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception,
            "Service Bus processor error. Source: {ErrorSource}, Entity: {EntityPath}",
            args.ErrorSource, args.EntityPath);
        return Task.CompletedTask;
    }

    private static TimeSpan CalculateBackoff(int deliveryCount)
    {
        // 2, 4, 8, 16, then capped at 30 seconds
        double seconds = Math.Pow(2, deliveryCount);
        return TimeSpan.FromSeconds(Math.Min(seconds, 30));
    }
}