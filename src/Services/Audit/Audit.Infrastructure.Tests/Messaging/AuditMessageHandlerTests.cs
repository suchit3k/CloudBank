using Audit.Domain;
using Audit.Infrastructure.Messaging;
using Audit.Infrastructure.Persistence;
using NSubstitute;
using Xunit;

namespace Audit.Infrastructure.Tests.Messaging;

public class AuditMessageHandlerTests
{
    private const string ValidPayload = "{\"TransactionId\":\"00000000-0000-0000-0000-000000000001\"}";
    private const string EventType = "TransactionCompletedEvent";

    private readonly IAuditEntryStore _store = Substitute.For<IAuditEntryStore>();
    private readonly AuditMessageHandler _handler;

    public AuditMessageHandlerTests()
    {
        _handler = new AuditMessageHandler(_store);
    }

    [Fact]
    public async Task HandleAsync_NewMessage_StoresItAndReturnsStored()
    {
        // Arrange
        _store.ExistsAsync("msg-1", Arg.Any<CancellationToken>()).Returns(false);
        _store.TryAddAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _handler.HandleAsync("msg-1", EventType, ValidPayload, CancellationToken.None);

        // Assert
        Assert.Equal(AuditHandlingOutcome.Stored, result.Outcome);
        await _store.Received(1).TryAddAsync(
            Arg.Is<AuditEntry>(e =>
                e.MessageId == "msg-1" &&
                e.EventType == EventType &&
                e.Payload == ValidPayload),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_AlreadyStored_ReturnsDuplicateWithoutAdding()
    {
        _store.ExistsAsync("msg-1", Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.HandleAsync("msg-1", EventType, ValidPayload, CancellationToken.None);

        Assert.Equal(AuditHandlingOutcome.Duplicate, result.Outcome);
        await _store.DidNotReceive().TryAddAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_DuplicateCaughtByConstraint_ReturnsDuplicate()
    {
        // The race: existence check passed, but another copy was inserted first
        _store.ExistsAsync("msg-1", Arg.Any<CancellationToken>()).Returns(false);
        _store.TryAddAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.HandleAsync("msg-1", EventType, ValidPayload, CancellationToken.None);

        Assert.Equal(AuditHandlingOutcome.Duplicate, result.Outcome);
    }

    [Theory]
    [InlineData(null, EventType)]
    [InlineData("", EventType)]
    [InlineData("msg-1", null)]
    [InlineData("msg-1", "   ")]
    public async Task HandleAsync_MissingMetadata_ReturnsInvalidWithoutTouchingStore(string? messageId, string? eventType)
    {
        var result = await _handler.HandleAsync(messageId, eventType, ValidPayload, CancellationToken.None);

        Assert.Equal(AuditHandlingOutcome.InvalidMessage, result.Outcome);
        Assert.Equal("MissingMetadata", result.Reason);
        await _store.DidNotReceive().ExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_InvalidJson_ReturnsInvalidWithoutTouchingStore()
    {
        var result = await _handler.HandleAsync("msg-1", EventType, "this is not json", CancellationToken.None);

        Assert.Equal(AuditHandlingOutcome.InvalidMessage, result.Outcome);
        Assert.Equal("InvalidPayload", result.Reason);
        await _store.DidNotReceive().ExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_StoreUnavailable_ThrowsSoTheMessageIsRetried()
    {
        // A transient failure must NOT be turned into "InvalidMessage" (which would dead-letter it)
        _store.ExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task<bool>>(_ => throw new TimeoutException("Database unavailable"));

        await Assert.ThrowsAsync<TimeoutException>(
            () => _handler.HandleAsync("msg-1", EventType, ValidPayload, CancellationToken.None));
    }
}