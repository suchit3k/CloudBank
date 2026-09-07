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
    }
}