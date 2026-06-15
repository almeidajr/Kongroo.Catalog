using Kongroo.BuildingBlocks.Domain.Exceptions;
using Kongroo.Catalog.Application;
using Kongroo.Catalog.Domain;
using Kongroo.Catalog.Infrastructure;
using Kongroo.Catalog.IntegrationTests.Fixtures;
using Shouldly;

namespace Kongroo.Catalog.IntegrationTests.Catalog.Application;

public sealed class GetGameOwnershipQueryHandlerTests(PostgreSqlFixture postgreSqlFixture)
    : IClassFixture<PostgreSqlFixture>,
        IAsyncLifetime
{
    private readonly CatalogTestDatabase _database = new(postgreSqlFixture);

    [Fact]
    public async Task HandleAsync_WithCurrentOwnerOwnership_ShouldReturnOwnership()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var ownerId = Guid.NewGuid();
        var ownership = await AcquireOwnershipAsync(
            context,
            ownerId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero),
            TestContext.Current.CancellationToken
        );
        var handler = new GetGameOwnershipQueryHandler(context);

        // Act
        var response = await handler.HandleAsync(
            new GetGameOwnershipQuery(ownerId, ownership.Id),
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
        var ownerId = Guid.NewGuid();
        var ownershipId = Guid.NewGuid();
        var handler = new GetGameOwnershipQueryHandler(context);

        // Act
        var exception = await Should.ThrowAsync<NotFoundException>(() =>
            handler.HandleAsync(new GetGameOwnershipQuery(ownerId, ownershipId), TestContext.Current.CancellationToken)
        );

        // Assert
        exception.ResourceName.ShouldBe(nameof(GameOwnership));
        exception.Lookup.ShouldBe($"identifier '{ownershipId}'");
    }

    [Fact]
    public async Task HandleAsync_WhenOwnershipBelongsToAnotherOwner_ShouldThrowNotFoundException()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var ownerId = Guid.NewGuid();
        var ownership = await AcquireOwnershipAsync(
            context,
            ownerId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero),
            TestContext.Current.CancellationToken
        );
        var otherOwnerId = Guid.NewGuid();
        var handler = new GetGameOwnershipQueryHandler(context);

        // Act
        var exception = await Should.ThrowAsync<NotFoundException>(() =>
            handler.HandleAsync(
                new GetGameOwnershipQuery(otherOwnerId, ownership.Id),
                TestContext.Current.CancellationToken
            )
        );

        // Assert
        exception.ResourceName.ShouldBe(nameof(GameOwnership));
        exception.Lookup.ShouldBe($"identifier '{ownership.Id}'");
    }

    public async ValueTask InitializeAsync() => await _database.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static async Task<GetGameOwnershipResponse> AcquireOwnershipAsync(
        CatalogDbContext context,
        Guid ownerId,
        Guid gameId,
        Guid orderId,
        DateTimeOffset acquiredAt,
        CancellationToken cancellationToken
    )
    {
        var ownership = GameOwnership.AcquireFromOrder(
            OwnerId.From(ownerId),
            GameId.From(gameId),
            OrderId.From(orderId),
            acquiredAt
        );
        context.GameOwnerships.Add(ownership);
        await context.SaveChangesAsync(cancellationToken);

        return new GetGameOwnershipResponse(
            ownership.Id.Value,
            ownership.OwnerId.Value,
            ownership.GameId.Value,
            ownership.OrderId.Value,
            ownership.AcquiredAt
        );
    }
}
