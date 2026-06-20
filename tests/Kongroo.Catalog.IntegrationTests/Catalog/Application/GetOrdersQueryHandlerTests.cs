using Kongroo.Catalog.Application;
using Kongroo.Catalog.Domain;
using Kongroo.Catalog.Infrastructure;
using Kongroo.Catalog.IntegrationTests.Fixtures;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace Kongroo.Catalog.IntegrationTests.Catalog.Application;

public sealed class GetOrdersQueryHandlerTests(PostgreSqlFixture postgreSqlFixture)
    : IClassFixture<PostgreSqlFixture>,
        IAsyncLifetime
{
    private const string Email = "ada@example.com";
    private const string CustomerName = "Ada Lovelace";
    private readonly CatalogTestDatabase _database = new(postgreSqlFixture);

    [Fact]
    public async Task HandleAsync_WithNoOrders_ShouldReturnEmptyList()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var handler = new GetOrdersQueryHandler(context);

        // Act
        var response = await handler.HandleAsync(
            new GetOrdersQuery(CustomerId.Create().Value),
            TestContext.Current.CancellationToken
        );

        // Assert
        response.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WithCurrentCustomerOrders_ShouldReturnOnlyCurrentCustomerOrdersOrderedByMostRecentFirst()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var firstGameId = await CreatePublishedGameAsync(context, "Portal", 20m, TestContext.Current.CancellationToken);
        var secondGameId = await CreatePublishedGameAsync(
            context,
            "Half-Life",
            15m,
            TestContext.Current.CancellationToken
        );
        var thirdGameId = await CreatePublishedGameAsync(
            context,
            "Celeste",
            25m,
            TestContext.Current.CancellationToken
        );
        var customerId = CustomerId.Create();
        var otherCustomerId = CustomerId.Create();

        var olderOrder = await PlaceOrderAsync(
            context,
            customerId,
            [firstGameId],
            new DateTimeOffset(2026, 4, 1, 10, 0, 0, TimeSpan.Zero),
            TestContext.Current.CancellationToken
        );
        await PlaceOrderAsync(
            context,
            otherCustomerId,
            [secondGameId],
            new DateTimeOffset(2026, 4, 1, 11, 0, 0, TimeSpan.Zero),
            TestContext.Current.CancellationToken
        );
        var newerOrder = await PlaceOrderAsync(
            context,
            customerId,
            [thirdGameId],
            new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero),
            TestContext.Current.CancellationToken
        );
        var handler = new GetOrdersQueryHandler(context);

        // Act
        var response = await handler.HandleAsync(
            new GetOrdersQuery(customerId.Value),
            TestContext.Current.CancellationToken
        );

        // Assert
        response.Select(order => order.Id).ShouldBe([newerOrder.Id, olderOrder.Id]);
        response.All(order => order.CustomerId == customerId.Value).ShouldBeTrue();
        response.ShouldAllBe(order => order.Status == OrderStatus.Pending);
    }

    [Fact]
    public async Task HandleAsync_WithCurrentCustomerOrders_ShouldIncludeFullLineSnapshots()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var purchasedAt = new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero);
        var gameId = await CreatePublishedGameAsync(context, "Portal", 20m, TestContext.Current.CancellationToken);
        var promotion = await CreatePromotionAsync(
            context,
            gameId,
            50m,
            purchasedAt.AddDays(-1),
            purchasedAt.AddDays(1),
            TestContext.Current.CancellationToken
        );
        var customerId = CustomerId.Create();
        await PlaceOrderAsync(context, customerId, [gameId], purchasedAt, TestContext.Current.CancellationToken);
        var handler = new GetOrdersQueryHandler(context);

        // Act
        var response = await handler.HandleAsync(
            new GetOrdersQuery(customerId.Value),
            TestContext.Current.CancellationToken
        );

        // Assert
        var line = response.Single().Lines.Single();
        line.ShouldSatisfyAllConditions(
            () => line.GameId.ShouldBe(gameId.Value),
            () => line.GameTitle.ShouldBe("Portal"),
            () => line.ListPriceAmount.ShouldBe(20m),
            () => line.ListPriceCurrency.ShouldBe(Currency.Usd),
            () => line.FinalPriceAmount.ShouldBe(10m),
            () => line.FinalPriceCurrency.ShouldBe(Currency.Usd),
            () => line.AppliedPromotionId.ShouldBe(promotion.Id)
        );
    }

    public async ValueTask InitializeAsync() => await _database.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

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

        var updateGameHandler = new UpdateGameCommandHandler(context, new FakeTimeProvider(DateTimeOffset.UtcNow));
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

    private static async Task<GetOrderResponse> PlaceOrderAsync(
        CatalogDbContext context,
        CustomerId customerId,
        IReadOnlyList<GameId> gameIds,
        DateTimeOffset purchasedAt,
        CancellationToken cancellationToken
    )
    {
        var handler = new PlaceOrderCommandHandler(context, new FakeTimeProvider(purchasedAt));
        return await handler.HandleAsync(
            new PlaceOrderCommand(customerId.Value, Email, CustomerName, [.. gameIds.Select(gameId => gameId.Value)]),
            cancellationToken
        );
    }
}
