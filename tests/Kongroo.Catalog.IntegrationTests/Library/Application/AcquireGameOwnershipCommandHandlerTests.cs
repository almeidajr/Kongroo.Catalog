using Kongroo.BuildingBlocks.Domain.Exceptions;
using Kongroo.Catalog.IntegrationTests.Fixtures;
using Kongroo.Catalog.Application;
using Kongroo.Catalog.Domain;
using Kongroo.Catalog.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Kongroo.Catalog.IntegrationTests.Library.Application;

public sealed class AcquireGameOwnershipCommandHandlerTests(PostgreSqlFixture postgreSqlFixture)
    : IClassFixture<PostgreSqlFixture>,
        IAsyncLifetime
{
    private readonly LibraryTestDatabase _database = new(postgreSqlFixture);

    [Fact]
    public async Task HandleAsync_WithNewOwnership_ShouldCreateAndPersistOwnership()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var ownerId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var acquiredAt = new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero);
        var handler = new AcquireGameOwnershipCommandHandler(context);

        // Act
        var response = await handler.HandleAsync(
            new AcquireGameOwnershipCommand(ownerId, gameId, orderId, acquiredAt),
            TestContext.Current.CancellationToken
        );

        // Assert
        response.ShouldSatisfyAllConditions(
            () => response.Id.ShouldNotBe(Guid.Empty),
            () => response.OwnerId.ShouldBe(ownerId),
            () => response.GameId.ShouldBe(gameId),
            () => response.OrderId.ShouldBe(orderId),
            () => response.AcquiredAt.ShouldBe(acquiredAt)
        );

        context.ChangeTracker.Clear();
        var savedOwnership = await context.GameOwnerships.SingleAsync(
            ownership => ownership.Id == GameOwnershipId.From(response.Id),
            TestContext.Current.CancellationToken
        );

        savedOwnership.ShouldSatisfyAllConditions(
            () => savedOwnership.OwnerId.ShouldBe(OwnerId.From(ownerId)),
            () => savedOwnership.GameId.ShouldBe(GameId.From(gameId)),
            () => savedOwnership.OrderId.ShouldBe(OrderId.From(orderId)),
            () => savedOwnership.AcquiredAt.ShouldBe(acquiredAt)
        );
    }

    [Fact]
    public async Task HandleAsync_WithDuplicateOwnerAndGame_ShouldThrowConflictException()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var ownerId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        await AcquireOwnershipAsync(
            context,
            ownerId,
            gameId,
            Guid.NewGuid(),
            new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero),
            TestContext.Current.CancellationToken
        );
        var handler = new AcquireGameOwnershipCommandHandler(context);

        // Act
        var exception = await Should.ThrowAsync<ConflictException>(() =>
            handler.HandleAsync(
                new AcquireGameOwnershipCommand(
                    ownerId,
                    gameId,
                    Guid.NewGuid(),
                    new DateTimeOffset(2026, 4, 1, 13, 0, 0, TimeSpan.Zero)
                ),
                TestContext.Current.CancellationToken
            )
        );

        // Assert
        exception.ResourceName.ShouldBe(nameof(GameOwnership));
        exception.Reason.ShouldBe($"owner already owns game '{gameId}'");
        context.ChangeTracker.Clear();
        (await context.GameOwnerships.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(1);
    }

    public async ValueTask InitializeAsync() => await _database.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static async Task<GetGameOwnershipResponse> AcquireOwnershipAsync(
        LibraryDbContext context,
        Guid ownerId,
        Guid gameId,
        Guid orderId,
        DateTimeOffset acquiredAt,
        CancellationToken cancellationToken
    )
    {
        var handler = new AcquireGameOwnershipCommandHandler(context);
        return await handler.HandleAsync(
            new AcquireGameOwnershipCommand(ownerId, gameId, orderId, acquiredAt),
            cancellationToken
        );
    }
}

