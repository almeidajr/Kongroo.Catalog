using Kongroo.Catalog.Application;
using Kongroo.Catalog.Domain;
using Kongroo.Catalog.Infrastructure;
using Kongroo.Catalog.IntegrationTests.Fixtures;
using Shouldly;

namespace Kongroo.Catalog.IntegrationTests.Catalog.Application;

public sealed class GetOwnershipsQueryHandlerTests(PostgreSqlFixture postgreSqlFixture)
    : IClassFixture<PostgreSqlFixture>,
        IAsyncLifetime
{
    private readonly CatalogTestDatabase _database = new(postgreSqlFixture);

    [Fact]
    public async Task HandleAsync_WithNoOwnerships_ShouldReturnEmptyList()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var handler = new GetOwnershipsQueryHandler(context);
        var customerId = Guid.NewGuid();

        // Act
        var response = await handler.HandleAsync(
            new GetOwnershipsQuery(customerId),
            TestContext.Current.CancellationToken
        );

        // Assert
        response.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WithCurrentCustomerOwnerships_ShouldReturnOnlyCurrentCustomerOwnershipsOrderedByMostRecentFirst()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var customerId = Guid.NewGuid();
        var otherCustomerId = Guid.NewGuid();

        var olderOwnership = await AcquireOwnershipAsync(
            context,
            customerId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateTimeOffset(2026, 4, 1, 10, 0, 0, TimeSpan.Zero),
            TestContext.Current.CancellationToken
        );
        await AcquireOwnershipAsync(
            context,
            otherCustomerId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateTimeOffset(2026, 4, 1, 11, 0, 0, TimeSpan.Zero),
            TestContext.Current.CancellationToken
        );
        var newerOwnership = await AcquireOwnershipAsync(
            context,
            customerId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero),
            TestContext.Current.CancellationToken
        );
        var handler = new GetOwnershipsQueryHandler(context);

        // Act
        var response = await handler.HandleAsync(
            new GetOwnershipsQuery(customerId),
            TestContext.Current.CancellationToken
        );

        // Assert
        response.Select(ownership => ownership.Id).ShouldBe([newerOwnership.Id, olderOwnership.Id]);
        response.All(ownership => ownership.CustomerId == customerId).ShouldBeTrue();
    }

    public async ValueTask InitializeAsync() => await _database.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static async Task<GetOwnershipResponse> AcquireOwnershipAsync(
        CatalogDbContext context,
        Guid customerId,
        Guid gameId,
        Guid orderId,
        DateTimeOffset acquiredAt,
        CancellationToken cancellationToken
    )
    {
        var ownership = Ownership.AcquireFromOrder(
            CustomerId.From(customerId),
            GameId.From(gameId),
            OrderId.From(orderId),
            acquiredAt
        );
        context.Ownerships.Add(ownership);
        await context.SaveChangesAsync(cancellationToken);

        return new GetOwnershipResponse(
            ownership.Id.Value,
            ownership.CustomerId.Value,
            ownership.GameId.Value,
            ownership.OrderId.Value,
            ownership.AcquiredAt
        );
    }
}
