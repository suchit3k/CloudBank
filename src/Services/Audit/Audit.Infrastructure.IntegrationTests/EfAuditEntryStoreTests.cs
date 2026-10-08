using Audit.Domain;
using Audit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Nodes;
using Xunit;

namespace Audit.Infrastructure.IntegrationTests;

public class EfAuditEntryStoreTests : IClassFixture<PostgresFixture>
{
    private const string EventType = "TransactionCompletedEvent";
    private readonly PostgresFixture _fixture;

    public EfAuditEntryStoreTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task TryAddAsync_NewEntry_PreservesPayloadContent()
    {
        string messageId = Guid.NewGuid().ToString();
        string payload = "{\"TransactionId\":\"abc\",\"Amount\":10}";

        await using (var dbContext = _fixture.CreateDbContext())
        {
            var store = new EfAuditEntryStore(dbContext);
            bool added = await store.TryAddAsync(new AuditEntry(messageId, EventType, payload), CancellationToken.None);
            Assert.True(added);
        }

        await using (var dbContext = _fixture.CreateDbContext())
        {
            var stored = await dbContext.AuditEntries.SingleAsync(a => a.MessageId == messageId);

            // jsonb keeps the content but not the exact text (key order and spacing may change),
            // which is the deliberate trade-off from Day 15. So compare as JSON, not as strings.
            Assert.True(
                JsonNode.DeepEquals(JsonNode.Parse(payload), JsonNode.Parse(stored.Payload)),
                $"Payload content changed. Stored: {stored.Payload}");
        }
    }

    [Fact]
    public async Task TryAddAsync_DuplicateMessageId_ReturnsFalse()
    {
        string messageId = Guid.NewGuid().ToString();
        string payload = "{\"TransactionId\":\"abc\"}";

        await using (var dbContext = _fixture.CreateDbContext())
        {
            await new EfAuditEntryStore(dbContext)
                .TryAddAsync(new AuditEntry(messageId, EventType, payload), CancellationToken.None);
        }

        // Second insert with the same MessageId, skipping ExistsAsync, to hit the unique constraint
        await using (var dbContext = _fixture.CreateDbContext())
        {
            bool addedAgain = await new EfAuditEntryStore(dbContext)
                .TryAddAsync(new AuditEntry(messageId, EventType, payload), CancellationToken.None);

            Assert.False(addedAgain);
        }
    }

    [Fact]
    public async Task ExistsAsync_UnknownMessageId_ReturnsFalse()
    {
        await using var dbContext = _fixture.CreateDbContext();

        bool exists = await new EfAuditEntryStore(dbContext)
            .ExistsAsync(Guid.NewGuid().ToString(), CancellationToken.None);

        Assert.False(exists);
    }

    [Fact]
    public async Task TryAddAsync_InvalidJson_IsRejectedByJsonbColumn()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var store = new EfAuditEntryStore(dbContext);

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            store.TryAddAsync(new AuditEntry(Guid.NewGuid().ToString(), EventType, "not json"), CancellationToken.None));
    }
}