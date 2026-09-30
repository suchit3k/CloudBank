using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Reflection.Emit;
using Transactions.Domain;

namespace Transactions.Infrastructure.Persistence;

public class TransactionDbContext : DbContext
{
    public TransactionDbContext(DbContextOptions<TransactionDbContext> options)
        : base(options)
    {
    }

    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.ToTable("transactions");

            entity.HasKey(t => t.Id);

            entity.Property(t => t.SenderAccountId).IsRequired();
            entity.Property(t => t.ReceiverAccountId).IsRequired();

            entity.Property(t => t.Amount)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(t => t.Currency)
                .HasMaxLength(3)
                .IsRequired();

            entity.Property(t => t.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(t => t.FailureReason).HasMaxLength(500);

            entity.Property(t => t.IdempotencyKey).IsRequired();
            entity.HasIndex(t => t.IdempotencyKey).IsUnique();

            entity.Property(t => t.CreatedAt).IsRequired();

            entity.HasIndex(t => t.SenderAccountId);
            entity.HasIndex(t => t.ReceiverAccountId);
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("outbox_messages");

            entity.HasKey(o => o.Id);

            entity.Property(o => o.EventType)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(o => o.Payload)
                    .IsRequired();

            entity.Property(o => o.CreatedAtUtc).IsRequired();
            entity.Property(o => o.AttemptCount).IsRequired();
            entity.Property(o => o.LastError).HasMaxLength(1000);

            // Fast lookup for "give me unpublished messages"
            entity.HasIndex(o => o.PublishedAtUtc);
        });
    }
}
