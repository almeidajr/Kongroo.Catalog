using Kongroo.BuildingBlocks.Domain.Exceptions;
using Kongroo.Catalog.Application;
using Kongroo.Catalog.Domain;
using Kongroo.Catalog.Infrastructure;
using Kongroo.Catalog.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace Kongroo.Catalog.IntegrationTests.Catalog.Application;

public sealed class UpdateGameCommandHandlerTests(PostgreSqlFixture postgreSqlFixture)
    : IClassFixture<PostgreSqlFixture>,
        IAsyncLifetime
{
    private readonly CatalogTestDatabase _database = new(postgreSqlFixture);

    [Fact]
    public async Task HandleAsync_WithExistingGame_ShouldReturnUpdatedGameResponse()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var gameId = await CreateGameAsync(context, TestContext.Current.CancellationToken);

        var handler = new UpdateGameCommandHandler(context, new FakeTimeProvider());

        // Act
        var response = await handler.HandleAsync(
            new UpdateGameCommand(
                gameId.Value,
                "Portal 2",
                "A cooperative puzzle platformer.",
                29.99m,
                Currency.Eur,
                GameStatus.Published
            ),
            TestContext.Current.CancellationToken
        );

        // Assert
        response.ShouldSatisfyAllConditions(
            () => response.Id.ShouldBe(gameId.Value),
            () => response.Title.ShouldBe("Portal 2"),
            () => response.Description.ShouldBe("A cooperative puzzle platformer."),
            () => response.PriceAmount.ShouldBe(29.99m),
            () => response.Currency.ShouldBe(Currency.Eur),
            () => response.Status.ShouldBe(GameStatus.Published)
        );
    }

    [Fact]
    public async Task HandleAsync_WithExistingGame_ShouldPersistUpdatedValues()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var gameId = await CreateGameAsync(context, TestContext.Current.CancellationToken);

        var handler = new UpdateGameCommandHandler(context, new FakeTimeProvider());

        // Act
        await handler.HandleAsync(
            new UpdateGameCommand(
                gameId.Value,
                "Portal 2",
                "A cooperative puzzle platformer.",
                29.99m,
                Currency.Eur,
                GameStatus.Published
            ),
            TestContext.Current.CancellationToken
        );

        context.ChangeTracker.Clear();
        var savedGame = await context.Games.SingleAsync(
            candidate => candidate.Id == gameId,
            TestContext.Current.CancellationToken
        );

        // Assert
        savedGame.ShouldSatisfyAllConditions(
            () => savedGame.Title.Value.ShouldBe("Portal 2"),
            () => savedGame.Description.Value.ShouldBe("A cooperative puzzle platformer."),
            () => savedGame.Price.Amount.ShouldBe(29.99m),
            () => savedGame.Price.Currency.ShouldBe(Currency.Eur),
            () => savedGame.Status.ShouldBe(GameStatus.Published)
        );
    }

    [Fact]
    public async Task HandleAsync_WhenGameDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var missingGameId = Guid.NewGuid();
        var handler = new UpdateGameCommandHandler(context, new FakeTimeProvider());

        // Act
        var exception = await Should.ThrowAsync<NotFoundException>(() =>
            handler.HandleAsync(
                new UpdateGameCommand(
                    missingGameId,
                    "Portal 2",
                    "A cooperative puzzle platformer.",
                    29.99m,
                    Currency.Eur,
                    GameStatus.Published
                ),
                TestContext.Current.CancellationToken
            )
        );

        // Assert
        exception.ResourceName.ShouldBe(nameof(Game));
        exception.Lookup.ShouldBe($"identifier '{missingGameId}'");
    }

    public async ValueTask InitializeAsync() => await _database.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static async Task<GameId> CreateGameAsync(CatalogDbContext context, CancellationToken cancellationToken)
    {
        var handler = new CreateGameCommandHandler(context);
        var response = await handler.HandleAsync(
            new CreateGameCommand("Portal", "A puzzle platformer.", 19.99m, Currency.Usd),
            cancellationToken
        );
        return new GameId(response.Id);
    }
}

