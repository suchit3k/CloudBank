using Audit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace Audit.Infrastructure.IntegrationTests;

public class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16")
     .Build();  

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Apply our REAL migrations, so the schema is tested too
        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public AuditDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuditDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;

        return new AuditDbContext(options);
    }
}