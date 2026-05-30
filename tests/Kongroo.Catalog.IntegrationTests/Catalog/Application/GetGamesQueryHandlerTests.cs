using Kongroo.Catalog.Application;
using Kongroo.Catalog.Domain;
using Kongroo.Catalog.Infrastructure;
using Kongroo.Catalog.IntegrationTests.Fixtures;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace Kongroo.Catalog.IntegrationTests.Catalog.Application;

public sealed class GetGamesQueryHandlerTests(PostgreSqlFixture postgreSqlFixture)
    : IClassFixture<PostgreSqlFixture>,
        IAsyncLifetime
{
    private static readonly DateTimeOffset ReadAt = new(2026, 4, 1, 12, 0, 0, TimeSpan.Zero);
    private readonly CatalogTestDatabase _database = new(postgreSqlFixture);

    [Fact]
    public async Task HandleAsync_WithNoGames_ShouldReturnEmptyList()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var handler = new GetGamesQueryHandler(context, new FakeTimeProvider(ReadAt));

        // Act
        var response = await handler.HandleAsync(new GetGamesQuery(), TestContext.Current.CancellationToken);

        // Assert
        response.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WithExistingGames_ShouldReturnGamesOrderedByTitle()
    {
        // Arrange
        await using var context = _database.CreateDbContext();

        await CreateGameAsync(
            new CreateGameCommand("Zelda", "Adventure game.", 59.99m, Currency.Usd),
            context,
            TestContext.Current.CancellationToken
        );
        await CreateGameAsync(
            new CreateGameCommand("Portal", "Puzzle platformer.", 19.99m, Currency.Eur),
            context,
            TestContext.Current.CancellationToken
        );
        await CreateGameAsync(
            new CreateGameCommand("Celeste", "Precision platformer.", 24.99m, Currency.Brl),
            context,
            TestContext.Current.CancellationToken
        );

        var handler = new GetGamesQueryHandler(context, new FakeTimeProvider(ReadAt));

        // Act
        var response = await handler.HandleAsync(new GetGamesQuery(), TestContext.Current.CancellationToken);

        // Assert
        response.Select(game => game.Title).ShouldBe(["Celeste", "Portal", "Zelda"]);
        response.All(game => game.ActivePromotion is null).ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAsync_WhenGameHasActivePromotion_ShouldReturnActivePromotion()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var gameId = await CreatePublishedGameAsync(context, "Portal", 19.99m, TestContext.Current.CancellationToken);
        var promotion = await CreatePromotionAsync(
            context,
            gameId,
            25m,
            ReadAt.AddDays(-1),
            ReadAt.AddDays(1),
            TestContext.Current.CancellationToken
        );
        var handler = new GetGamesQueryHandler(context, new FakeTimeProvider(ReadAt));

        // Act
        var response = await handler.HandleAsync(new GetGamesQuery(), TestContext.Current.CancellationToken);

        // Assert
        var game = response.Single(candidate => candidate.Id == gameId.Value);

        game.ActivePromotion.ShouldNotBeNull();

        game.ActivePromotion.ShouldSatisfyAllConditions(
            () => game.ActivePromotion.Id.ShouldBe(promotion.Id),
            () => game.ActivePromotion.GameId.ShouldBe(gameId.Value),
            () => game.ActivePromotion.Discount.ShouldBe(25m),
            () => game.ActivePromotion.StartsAt.ShouldBe(ReadAt.AddDays(-1)),
            () => game.ActivePromotion.EndsAt.ShouldBe(ReadAt.AddDays(1))
        );
    }

    [Fact]
    public async Task HandleAsync_WhenGameHasOnlyExpiredPromotion_ShouldIgnoreExpiredPromotion()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var gameId = await CreatePublishedGameAsync(context, "Portal", 19.99m, TestContext.Current.CancellationToken);
        await CreatePromotionAsync(
            context,
            gameId,
            25m,
            ReadAt.AddDays(-10),
            ReadAt.AddDays(-1),
            TestContext.Current.CancellationToken
        );
        var handler = new GetGamesQueryHandler(context, new FakeTimeProvider(ReadAt));

        // Act
        var response = await handler.HandleAsync(new GetGamesQuery(), TestContext.Current.CancellationToken);

        // Assert
        response.Single(candidate => candidate.Id == gameId.Value).ActivePromotion.ShouldBeNull();
    }

    public async ValueTask InitializeAsync() => await _database.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static async Task CreateGameAsync(
        CreateGameCommand command,
        CatalogDbContext context,
        CancellationToken cancellationToken
    )
    {
        var handler = new CreateGameCommandHandler(context);
        await handler.HandleAsync(command, cancellationToken);
    }

    private static async Task<GameId> CreatePublishedGameAsync(
        CatalogDbContext context,
        string title,
        decimal priceAmount,
        CancellationToken cancellationToken
    )
    {
        var createGameHandler = new CreateGameCommandHandler(context);
        var createGameResponse = await createGameHandler.HandleAsync(
            new CreateGameCommand(title, $"{title} description.", priceAmount, Currency.Usd),
            cancellationToken
        );

        var updateGameHandler = new UpdateGameCommandHandler(context, new FakeTimeProvider(ReadAt));
        await updateGameHandler.HandleAsync(
            new UpdateGameCommand(
                createGameResponse.Id,
                title,
                $"{title} description.",
                priceAmount,
                Currency.Usd,
                GameStatus.Published
            ),
            cancellationToken
        );

        return GameId.From(createGameResponse.Id);
    }

    private static async Task<GetPromotionResponse> CreatePromotionAsync(
        CatalogDbContext context,
        GameId gameId,
        decimal discount,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        CancellationToken cancellationToken
    )
    {
        var handler = new CreatePromotionCommandHandler(context);
        return await handler.HandleAsync(
            new CreatePromotionCommand(gameId.Value, discount, startsAt, endsAt),
            cancellationToken
        );
    }
}
