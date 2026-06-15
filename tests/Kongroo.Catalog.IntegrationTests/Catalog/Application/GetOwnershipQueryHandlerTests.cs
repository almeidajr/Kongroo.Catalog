using Kongroo.BuildingBlocks.Domain.Exceptions;
using Kongroo.Catalog.Application;
using Kongroo.Catalog.Domain;
using Kongroo.Catalog.Infrastructure;
using Kongroo.Catalog.IntegrationTests.Fixtures;
using Shouldly;

namespace Kongroo.Catalog.IntegrationTests.Catalog.Application;

public sealed class GetOwnershipQueryHandlerTests(PostgreSqlFixture postgreSqlFixture)
    : IClassFixture<PostgreSqlFixture>,
        IAsyncLifetime
{
    private readonly CatalogTestDatabase _database = new(postgreSqlFixture);

    [Fact]
    public async Task HandleAsync_WithCurrentCustomerOwnership_ShouldReturnOwnership()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var customerId = Guid.NewGuid();
        var ownership = await AcquireOwnershipAsync(
            context,
            customerId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero),
            TestContext.Current.CancellationToken
        );
        var handler = new GetOwnershipQueryHandler(context);

        // Act
        var response = await handler.HandleAsync(
            new GetOwnershipQuery(customerId, ownership.Id),
            TestContext.Current.CancellationToken
        );

        // Assert
        response.ShouldBe(ownership);
    }

    [Fact]
    public async Task HandleAsync_WhenOwnershipDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var customerId = Guid.NewGuid();
        var ownershipId = Guid.NewGuid();
        var handler = new GetOwnershipQueryHandler(context);

        // Act
        var exception = await Should.ThrowAsync<NotFoundException>(() =>
            handler.HandleAsync(new GetOwnershipQuery(customerId, ownershipId), TestContext.Current.CancellationToken)
        );

        // Assert
        exception.ResourceName.ShouldBe(nameof(Ownership));
        exception.Lookup.ShouldBe($"identifier '{ownershipId}'");
    }

    [Fact]
    public async Task HandleAsync_WhenOwnershipBelongsToAnotherCustomer_ShouldThrowNotFoundException()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var customerId = Guid.NewGuid();
        var ownership = await AcquireOwnershipAsync(
            context,
            customerId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero),
            TestContext.Current.CancellationToken
        );
        var otherCustomerId = Guid.NewGuid();
        var handler = new GetOwnershipQueryHandler(context);

        // Act
        var exception = await Should.ThrowAsync<NotFoundException>(() =>
            handler.HandleAsync(
                new GetOwnershipQuery(otherCustomerId, ownership.Id),
                TestContext.Current.CancellationToken
            )
        );

        // Assert
        exception.ResourceName.ShouldBe(nameof(Ownership));
        exception.Lookup.ShouldBe($"identifier '{ownership.Id}'");
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
