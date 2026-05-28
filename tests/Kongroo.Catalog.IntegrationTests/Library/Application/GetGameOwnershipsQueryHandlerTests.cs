using Kongroo.Catalog.IntegrationTests.Fixtures;
using Kongroo.Catalog.Application;
using Kongroo.Catalog.Infrastructure;
using Shouldly;

namespace Kongroo.Catalog.IntegrationTests.Library.Application;

public sealed class GetGameOwnershipsQueryHandlerTests(PostgreSqlFixture postgreSqlFixture)
    : IClassFixture<PostgreSqlFixture>,
        IAsyncLifetime
{
    private readonly LibraryTestDatabase _database = new(postgreSqlFixture);

    [Fact]
    public async Task HandleAsync_WithNoOwnerships_ShouldReturnEmptyList()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var handler = new GetGameOwnershipsQueryHandler(context);
        var ownerId = Guid.NewGuid();

        // Act
        var response = await handler.HandleAsync(
            new GetGameOwnershipsQuery(ownerId),
            TestContext.Current.CancellationToken
        );

        // Assert
        response.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WithCurrentOwnerOwnerships_ShouldReturnOnlyCurrentOwnerOwnershipsOrderedByMostRecentFirst()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var ownerId = Guid.NewGuid();
        var otherOwnerId = Guid.NewGuid();

        var olderOwnership = await AcquireOwnershipAsync(
            context,
            ownerId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateTimeOffset(2026, 4, 1, 10, 0, 0, TimeSpan.Zero),
            TestContext.Current.CancellationToken
        );
        await AcquireOwnershipAsync(
            context,
            otherOwnerId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateTimeOffset(2026, 4, 1, 11, 0, 0, TimeSpan.Zero),
            TestContext.Current.CancellationToken
        );
        var newerOwnership = await AcquireOwnershipAsync(
            context,
            ownerId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero),
            TestContext.Current.CancellationToken
        );
        var handler = new GetGameOwnershipsQueryHandler(context);

        // Act
        var response = await handler.HandleAsync(
            new GetGameOwnershipsQuery(ownerId),
            TestContext.Current.CancellationToken
        );

        // Assert
        response.Select(ownership => ownership.Id).ShouldBe([newerOwnership.Id, olderOwnership.Id]);
        response.All(ownership => ownership.OwnerId == ownerId).ShouldBeTrue();
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

