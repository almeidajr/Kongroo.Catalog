using Kongroo.BuildingBlocks.Infrastructure;
using Kongroo.Catalog.Infrastructure;
using Kongroo.Catalog.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace Kongroo.Catalog.IntegrationTests.Catalog;

public sealed class CatalogTestDatabase(PostgreSqlFixture fixture)
{
    public CatalogDbContext CreateDbContext() =>
        new(
            new DbContextOptionsBuilder<CatalogDbContext>()
                .EnableDetailedErrors()
                .EnableSensitiveDataLogging()
                .AddInterceptors(new OutboxMessagesInterceptor())
                .UseNpgsql(
                    fixture.ConnectionString,
                    postgresOptions => postgresOptions.MigrationsHistoryTable("migrations", CatalogDbContext.Schema)
                )
                .UseSnakeCaseNamingConvention()
                .Options
        );

    public async Task ResetAsync(CancellationToken cancellationToken)
    {
        await using var context = CreateDbContext();
        await context.Database.MigrateAsync(cancellationToken);
        var truncateTablesSql = $"""
            TRUNCATE TABLE
                "{CatalogDbContext.Schema}"."outbox_messages",
                "{CatalogDbContext.Schema}"."order_lines",
                "{CatalogDbContext.Schema}"."orders",
                "{CatalogDbContext.Schema}"."promotions",
                "{CatalogDbContext.Schema}"."games"
            CASCADE;
            """;
        await context.Database.ExecuteSqlRawAsync(truncateTablesSql, cancellationToken);
    }
}

