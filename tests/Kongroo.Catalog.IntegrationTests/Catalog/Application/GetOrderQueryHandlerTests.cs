using Kongroo.BuildingBlocks.Domain.Exceptions;
using Kongroo.Catalog.Application;
using Kongroo.Catalog.Domain;
using Kongroo.Catalog.Infrastructure;
using Kongroo.Catalog.IntegrationTests.Fixtures;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace Kongroo.Catalog.IntegrationTests.Catalog.Application;

public sealed class GetOrderQueryHandlerTests(PostgreSqlFixture postgreSqlFixture)
    : IClassFixture<PostgreSqlFixture>,
        IAsyncLifetime
{
    private readonly CatalogTestDatabase _database = new(postgreSqlFixture);

    [Fact]
    public async Task HandleAsync_WithOwnedOrderId_ShouldReturnOrder()
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
        var buyerId = BuyerId.Create();
        var order = await PlaceOrderAsync(
            context,
            buyerId,
            [gameId],
            purchasedAt,
            TestContext.Current.CancellationToken
        );
        var handler = new GetOrderQueryHandler(context);

        // Act
        var response = await handler.HandleAsync(
            new GetOrderQuery(buyerId.Value, order.Id),
            TestContext.Current.CancellationToken
        );

        // Assert
        response.ShouldSatisfyAllConditions(
            () => response.Id.ShouldBe(order.Id),
            () => response.BuyerId.ShouldBe(buyerId.Value),
            () => response.PurchasedAt.ShouldBe(purchasedAt),
            () => response.TotalAmount.ShouldBe(10m),
            () => response.Currency.ShouldBe(Currency.Usd),
            () => response.Lines.Count.ShouldBe(1)
        );

        var line = response.Lines.Single();
        line.ShouldSatisfyAllConditions(
            () => line.GameId.ShouldBe(gameId.Value),
            () => line.GameTitle.ShouldBe("Portal"),
            () => line.ListPriceAmount.ShouldBe(20m),
            () => line.FinalPriceAmount.ShouldBe(10m),
            () => line.AppliedPromotionId.ShouldBe(promotion.Id)
        );
    }

    [Fact]
    public async Task HandleAsync_WhenOrderDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var missingOrderId = Guid.NewGuid();
        var handler = new GetOrderQueryHandler(context);

        // Act
        var exception = await Should.ThrowAsync<NotFoundException>(() =>
            handler.HandleAsync(
                new GetOrderQuery(BuyerId.Create().Value, missingOrderId),
                TestContext.Current.CancellationToken
            )
        );

        // Assert
        exception.ResourceName.ShouldBe(nameof(Order));
        exception.Lookup.ShouldBe($"identifier '{missingOrderId}'");
    }

    [Fact]
    public async Task HandleAsync_WhenOrderBelongsToDifferentBuyer_ShouldThrowNotFoundException()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var gameId = await CreatePublishedGameAsync(context, "Portal", 20m, TestContext.Current.CancellationToken);
        var ownerId = BuyerId.Create();
        var otherBuyerId = BuyerId.Create();
        var order = await PlaceOrderAsync(
            context,
            ownerId,
            [gameId],
            new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero),
            TestContext.Current.CancellationToken
        );
        var handler = new GetOrderQueryHandler(context);

        // Act
        var exception = await Should.ThrowAsync<NotFoundException>(() =>
            handler.HandleAsync(new GetOrderQuery(otherBuyerId.Value, order.Id), TestContext.Current.CancellationToken)
        );

        // Assert
        exception.ResourceName.ShouldBe(nameof(Order));
        exception.Lookup.ShouldBe($"identifier '{order.Id}'");
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
        BuyerId buyerId,
        IReadOnlyList<GameId> gameIds,
        DateTimeOffset purchasedAt,
        CancellationToken cancellationToken
    )
    {
        var handler = new PlaceOrderCommandHandler(context, new FakeTimeProvider(purchasedAt));
        return await handler.HandleAsync(
            new PlaceOrderCommand(buyerId.Value, [.. gameIds.Select(gameId => gameId.Value)]),
            cancellationToken
        );
    }
}

