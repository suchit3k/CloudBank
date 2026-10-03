using System.Text.Json;
using Audit.Domain;
using Audit.Infrastructure.Persistence;
using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
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
        string messageId = message.MessageId;
        string eventType = message.Subject;
        string payload = message.Body.ToString();

        // 1. Poison-message checks: these will never succeed, so retrying is pointless
        if (string.IsNullOrWhiteSpace(messageId) || string.IsNullOrWhiteSpace(eventType))
        {
            await args.DeadLetterMessageAsync(message, "MissingMetadata",
                "Message has no MessageId or Subject.", args.CancellationToken);
            _logger.LogWarning("Dead-lettered message with missing metadata.");
            return;
        }

        try
        {
            JsonDocument.Parse(payload).Dispose();
        }
        catch (JsonException ex)
        {
            await args.DeadLetterMessageAsync(message, "InvalidPayload", ex.Message, args.CancellationToken);
            _logger.LogWarning("Dead-lettered message {MessageId}: payload is not valid JSON.", messageId);
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AuditDbContext>();

            // 2. Idempotency check: have we already stored this event occurrence?
            bool alreadyStored = await dbContext.AuditEntries
                .AnyAsync(a => a.MessageId == messageId, args.CancellationToken);

            if (alreadyStored)
            {
                _logger.LogInformation("Duplicate message {MessageId} ignored.", messageId);
                await args.CompleteMessageAsync(message, args.CancellationToken);
                return;
            }

            // 3. Save first...
            dbContext.AuditEntries.Add(new AuditEntry(messageId, eventType, payload));

            try
            {
                await dbContext.SaveChangesAsync(args.CancellationToken);
                _logger.LogInformation("Stored audit entry for {EventType} (MessageId {MessageId}).", eventType, messageId);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                // Safety net: another copy was saved between our check and our insert
                _logger.LogInformation("Duplicate message {MessageId} caught by unique constraint.", messageId);
            }

            // 4. ...then complete (at-least-once)
            await args.CompleteMessageAsync(message, args.CancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Temporary problem (e.g. database down): release the lock so it's redelivered
            _logger.LogError(ex, "Failed to process message {MessageId}; abandoning for retry.", messageId);
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
}