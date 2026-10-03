using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Transactions.Application.Services;
using Transactions.Infrastructure.Persistence;

namespace Transactions.Infrastructure.Outbox;

public class OutboxPublisherBackgroundService : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);
    private const int BatchSize = 20;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<OutboxPublisherBackgroundService> _logger;

    public OutboxPublisherBackgroundService(
        IServiceScopeFactory scopeFactory,
        IEventPublisher eventPublisher,
        ILogger<OutboxPublisherBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox publisher started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishPendingMessagesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Never let one bad cycle kill the whole background service
                _logger.LogError(ex, "Outbox publishing cycle failed.");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }
    }

    private async Task PublishPendingMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TransactionDbContext>();

        var pendingMessages = await dbContext.OutboxMessages
            .Where(o => o.PublishedAtUtc == null)
            .OrderBy(o => o.CreatedAtUtc)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var message in pendingMessages)
        {
            try
            {
                await _eventPublisher.PublishAsync(message.Id.ToString(), message.EventType, message.Payload, cancellationToken);
                message.MarkAsPublished();
                _logger.LogInformation("Published outbox message {MessageId} ({EventType}).", message.Id, message.EventType);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                message.RecordFailedAttempt(ex.Message);
                _logger.LogWarning("Failed to publish outbox message {MessageId}, attempt {Attempt}: {Error}",
                    message.Id, message.AttemptCount, ex.Message);
            }

            // Save after EACH message, so one success is never lost because a later one failed
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}