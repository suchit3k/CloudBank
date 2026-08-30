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
                .HasColumnType("decimal(18,2)")
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
    }
}