using Kongroo.BuildingBlocks.Domain.Exceptions;
using Kongroo.Catalog.Application;
using Kongroo.Catalog.Domain;
using Kongroo.Catalog.Infrastructure;
using Kongroo.Catalog.IntegrationTests.Fixtures;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace Kongroo.Catalog.IntegrationTests.Catalog.Application;

public sealed class GetGameQueryHandlerTests(PostgreSqlFixture postgreSqlFixture)
    : IClassFixture<PostgreSqlFixture>,
        IAsyncLifetime
{
    private static readonly DateTimeOffset ReadAt = new(2026, 4, 1, 12, 0, 0, TimeSpan.Zero);
    private readonly CatalogTestDatabase _database = new(postgreSqlFixture);

    [Fact]
    public async Task HandleAsync_WithExistingGameId_ShouldReturnGame()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var gameId = await CreateGameAsync(context, TestContext.Current.CancellationToken);
        var handler = new GetGameQueryHandler(context, new FakeTimeProvider(ReadAt));

        // Act
        var response = await handler.HandleAsync(new GetGameQuery(gameId.Value), TestContext.Current.CancellationToken);

        // Assert
        response.ShouldSatisfyAllConditions(
            () => response.Id.ShouldBe(gameId.Value),
            () => response.Title.ShouldBe("Portal"),
            () => response.Description.ShouldBe("A puzzle platformer."),
            () => response.PriceAmount.ShouldBe(19.99m),
            () => response.Currency.ShouldBe(Currency.Usd),
            () => response.Status.ShouldBe(GameStatus.Draft),
            () => response.ActivePromotion.ShouldBeNull()
        );
    }

    [Fact]
    public async Task HandleAsync_WhenGameHasActivePromotion_ShouldReturnGameWithActivePromotion()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var gameId = await CreatePublishedGameAsync(context, TestContext.Current.CancellationToken);
        var promotion = await CreatePromotionAsync(
            context,
            gameId,
            25m,
            ReadAt.AddDays(-1),
            ReadAt.AddDays(1),
            TestContext.Current.CancellationToken
        );
        var handler = new GetGameQueryHandler(context, new FakeTimeProvider(ReadAt));

        // Act
        var response = await handler.HandleAsync(new GetGameQuery(gameId.Value), TestContext.Current.CancellationToken);

        // Assert
        response.ActivePromotion.ShouldNotBeNull();
        response.ActivePromotion.ShouldSatisfyAllConditions(
            () => response.ActivePromotion.Id.ShouldBe(promotion.Id),
            () => response.ActivePromotion.GameId.ShouldBe(gameId.Value),
            () => response.ActivePromotion.Discount.ShouldBe(25m),
            () => response.ActivePromotion.StartsAt.ShouldBe(ReadAt.AddDays(-1)),
            () => response.ActivePromotion.EndsAt.ShouldBe(ReadAt.AddDays(1))
        );
    }

    [Fact]
    public async Task HandleAsync_WhenGameHasNoActivePromotion_ShouldReturnNullActivePromotion()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var gameId = await CreatePublishedGameAsync(context, TestContext.Current.CancellationToken);
        await CreatePromotionAsync(
            context,
            gameId,
            25m,
            ReadAt.AddDays(-10),
            ReadAt.AddDays(-1),
            TestContext.Current.CancellationToken
        );
        var handler = new GetGameQueryHandler(context, new FakeTimeProvider(ReadAt));

        // Act
        var response = await handler.HandleAsync(new GetGameQuery(gameId.Value), TestContext.Current.CancellationToken);

        // Assert
        response.ActivePromotion.ShouldBeNull();
    }

    [Fact]
    public async Task HandleAsync_WhenGameDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var missingGameId = Guid.NewGuid();
        var handler = new GetGameQueryHandler(context, new FakeTimeProvider(ReadAt));

        // Act
        var exception = await Should.ThrowAsync<NotFoundException>(() =>
            handler.HandleAsync(new GetGameQuery(missingGameId), TestContext.Current.CancellationToken)
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

        return GameId.From(response.Id);
    }

    private static async Task<GameId> CreatePublishedGameAsync(
        CatalogDbContext context,
        CancellationToken cancellationToken
    )
    {
        var gameId = await CreateGameAsync(context, cancellationToken);
        var handler = new UpdateGameCommandHandler(context, new FakeTimeProvider(ReadAt));
        await handler.HandleAsync(
            new UpdateGameCommand(
                gameId.Value,
                "Portal",
                "A puzzle platformer.",
                19.99m,
                Currency.Usd,
                GameStatus.Published
            ),
            cancellationToken
        );

        return gameId;
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
