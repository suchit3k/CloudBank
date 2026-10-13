using Accounts.Domain;
using Microsoft.EntityFrameworkCore;

namespace Accounts.Infrastructure.Persistence;

public class AccountsDbContext : DbContext
{
    public AccountsDbContext(DbContextOptions<AccountsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<AccountOperation> AccountOperations => Set<AccountOperation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.ToTable("accounts");

            entity.HasKey(a => a.Id);

            entity.Property(a => a.UserId)
                .IsRequired();

            entity.Property(a => a.AccountType)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(a => a.Balance)
                .HasPrecision(18, 2)
                .IsRequired();


            entity.Property(a => a.Currency)
                .HasMaxLength(3)
                .IsRequired();

            entity.Property(a => a.CreatedAt)
                .IsRequired();

            entity.Property(a => a.IsFrozen)
                .IsRequired();

            entity.HasIndex(a => a.UserId); // fast lookup: "get all accounts for this user"
        });

        modelBuilder.Entity<AccountOperation>(entity =>
        {
            entity.ToTable("account_operations");

            entity.HasKey(o => o.Id);

            entity.Property(o => o.OperationId)
                .HasMaxLength(100)
                .IsRequired();

            // The idempotency guarantee: one operation can never be recorded twice
            entity.HasIndex(o => o.OperationId).IsUnique();

            entity.Property(o => o.Type)
                .HasConversion<string>()
                .HasMaxLength(10)
                .IsRequired();

            entity.Property(o => o.Amount)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(o => o.CreatedAtUtc).IsRequired();

            // Every operation belongs to a real account in this same database
            entity.HasOne<Account>()
                .WithMany()
                .HasForeignKey(o => o.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(o => o.AccountId);
        });
    }
}