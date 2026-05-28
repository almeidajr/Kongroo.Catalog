using Kongroo.BuildingBlocks.Infrastructure;
using Kongroo.Catalog.IntegrationTests.Fixtures;
using Kongroo.Catalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Kongroo.Catalog.IntegrationTests.Library;

public sealed class LibraryTestDatabase(PostgreSqlFixture fixture)
{
    public LibraryDbContext CreateDbContext() =>
        new(
            new DbContextOptionsBuilder<LibraryDbContext>()
                .EnableDetailedErrors()
                .EnableSensitiveDataLogging()
                .AddInterceptors(new OutboxMessagesInterceptor())
                .UseNpgsql(
                    fixture.ConnectionString,
                    postgresOptions => postgresOptions.MigrationsHistoryTable("migrations", LibraryDbContext.Schema)
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
                "{LibraryDbContext.Schema}"."outbox_messages",
                "{LibraryDbContext.Schema}"."game_ownerships"
            CASCADE;
            """;
        await context.Database.ExecuteSqlRawAsync(truncateTablesSql, cancellationToken);
    }
}

