using Audit.Domain;
using Microsoft.EntityFrameworkCore;

namespace Audit.Infrastructure.Persistence;

public class AuditDbContext : DbContext
{
    public AuditDbContext(DbContextOptions<AuditDbContext> options)
        : base(options)
    {
    }

    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditEntry>(entity =>
        {
            entity.ToTable("audit_entries");

            entity.HasKey(a => a.Id);

            entity.Property(a => a.MessageId)
                .HasMaxLength(100)
                .IsRequired();

            // The idempotency guarantee, enforced by Postgres itself
            entity.HasIndex(a => a.MessageId).IsUnique();

            entity.Property(a => a.EventType)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(a => a.Payload)
                .HasColumnType("jsonb")
                .IsRequired();

            entity.Property(a => a.ReceivedAtUtc).IsRequired();

            entity.HasIndex(a => a.EventType);
        });
    }
}