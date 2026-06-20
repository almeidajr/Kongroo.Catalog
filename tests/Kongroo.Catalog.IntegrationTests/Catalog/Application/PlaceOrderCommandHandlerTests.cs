using Kongroo.BuildingBlocks.Domain.Exceptions;
using Kongroo.Catalog.Application;
using Kongroo.Catalog.Domain;
using Kongroo.Catalog.Infrastructure;
using Kongroo.Catalog.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace Kongroo.Catalog.IntegrationTests.Catalog.Application;

public sealed class PlaceOrderCommandHandlerTests(PostgreSqlFixture postgreSqlFixture)
    : IClassFixture<PostgreSqlFixture>,
        IAsyncLifetime
{
    private static readonly DateTimeOffset PurchasedAt = new(2026, 4, 1, 12, 0, 0, TimeSpan.Zero);
    private const string Email = "ada@example.com";
    private const string CustomerName = "Ada Lovelace";
    private readonly CatalogTestDatabase _database = new(postgreSqlFixture);

    [Fact]
    public async Task HandleAsync_WithPurchasableGames_ShouldReturnPendingOrderResponse()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var discountedGameId = await CreatePublishedGameAsync(
            context,
            "Portal",
            20m,
            TestContext.Current.CancellationToken
        );
        var discountedPromotion = await CreatePromotionAsync(
            context,
            discountedGameId,
            50m,
            PurchasedAt.AddDays(-1),
            PurchasedAt.AddDays(1),
            TestContext.Current.CancellationToken
        );
        var fullPriceGameId = await CreatePublishedGameAsync(
            context,
            "Half-Life",
            15m,
            TestContext.Current.CancellationToken
        );
        var customerId = CustomerId.Create();
        var handler = new PlaceOrderCommandHandler(context, new FakeTimeProvider(PurchasedAt));

        // Act
        var response = await handler.HandleAsync(
            new PlaceOrderCommand(
                customerId.Value,
                Email,
                CustomerName,
                [discountedGameId.Value, fullPriceGameId.Value]
            ),
            TestContext.Current.CancellationToken
        );

        // Assert
        response.ShouldSatisfyAllConditions(
            () => response.Id.ShouldNotBe(Guid.Empty),
            () => response.CustomerId.ShouldBe(customerId.Value),
            () => response.Status.ShouldBe(OrderStatus.Pending),
            () => response.PurchasedAt.ShouldBe(PurchasedAt),
            () => response.TotalAmount.ShouldBe(25m),
            () => response.Currency.ShouldBe(Currency.Usd),
            () => response.Lines.Count.ShouldBe(2)
        );

        var discountedLine = response.Lines.Single(line => line.GameId == discountedGameId.Value);
        var fullPriceLine = response.Lines.Single(line => line.GameId == fullPriceGameId.Value);

        discountedLine.ShouldSatisfyAllConditions(
            () => discountedLine.GameTitle.ShouldBe("Portal"),
            () => discountedLine.ListPriceAmount.ShouldBe(20m),
            () => discountedLine.ListPriceCurrency.ShouldBe(Currency.Usd),
            () => discountedLine.FinalPriceAmount.ShouldBe(10m),
            () => discountedLine.FinalPriceCurrency.ShouldBe(Currency.Usd),
            () => discountedLine.AppliedPromotionId.ShouldBe(discountedPromotion.Id)
        );
        fullPriceLine.ShouldSatisfyAllConditions(
            () => fullPriceLine.GameTitle.ShouldBe("Half-Life"),
            () => fullPriceLine.ListPriceAmount.ShouldBe(15m),
            () => fullPriceLine.ListPriceCurrency.ShouldBe(Currency.Usd),
            () => fullPriceLine.FinalPriceAmount.ShouldBe(15m),
            () => fullPriceLine.FinalPriceCurrency.ShouldBe(Currency.Usd),
            () => fullPriceLine.AppliedPromotionId.ShouldBeNull()
        );
    }

    [Fact]
    public async Task HandleAsync_WithPurchasableGames_ShouldPersistPendingOrder()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var discountedGameId = await CreatePublishedGameAsync(
            context,
            "Portal",
            20m,
            TestContext.Current.CancellationToken
        );
        await CreatePromotionAsync(
            context,
            discountedGameId,
            50m,
            PurchasedAt.AddDays(-1),
            PurchasedAt.AddDays(1),
            TestContext.Current.CancellationToken
        );
        var fullPriceGameId = await CreatePublishedGameAsync(
            context,
            "Half-Life",
            15m,
            TestContext.Current.CancellationToken
        );
        var customerId = CustomerId.Create();
        var handler = new PlaceOrderCommandHandler(context, new FakeTimeProvider(PurchasedAt));

        // Act
        var response = await handler.HandleAsync(
            new PlaceOrderCommand(
                customerId.Value,
                Email,
                CustomerName,
                [discountedGameId.Value, fullPriceGameId.Value]
            ),
            TestContext.Current.CancellationToken
        );

        // Assert
        context.ChangeTracker.Clear();
        var savedOrder = await context
            .Orders.Include(order => order.Lines)
            .SingleAsync(order => order.Id == OrderId.From(response.Id), TestContext.Current.CancellationToken);

        savedOrder.ShouldSatisfyAllConditions(
            () => savedOrder.CustomerId.ShouldBe(customerId),
            () => savedOrder.Status.ShouldBe(OrderStatus.Pending),
            () => savedOrder.PurchasedAt.ShouldBe(PurchasedAt),
            () => savedOrder.Total.Amount.ShouldBe(25m),
            () => savedOrder.Total.Currency.ShouldBe(Currency.Usd),
            () => savedOrder.Lines.Count.ShouldBe(2)
        );

        var savedDiscountedLine = savedOrder.Lines.Single(line => line.GameId == discountedGameId);
        savedDiscountedLine.ShouldSatisfyAllConditions(
            () => savedDiscountedLine.GameTitle.Value.ShouldBe("Portal"),
            () => savedDiscountedLine.ListPrice.Amount.ShouldBe(20m),
            () => savedDiscountedLine.FinalPrice.Amount.ShouldBe(10m),
            () => savedDiscountedLine.AppliedPromotionId.ShouldNotBeNull()
        );
    }

    [Fact]
    public async Task HandleAsync_WhenGameHasExpiredAndActivePromotions_ShouldApplyActivePromotion()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var gameId = await CreatePublishedGameAsync(context, "Portal", 20m, TestContext.Current.CancellationToken);
        await CreatePromotionAsync(
            context,
            gameId,
            25m,
            PurchasedAt.AddDays(-10),
            PurchasedAt.AddDays(-1),
            TestContext.Current.CancellationToken
        );
        var activePromotion = await CreatePromotionAsync(
            context,
            gameId,
            50m,
            PurchasedAt.AddHours(-1),
            PurchasedAt.AddDays(1),
            TestContext.Current.CancellationToken
        );
        var customerId = CustomerId.Create();
        var handler = new PlaceOrderCommandHandler(context, new FakeTimeProvider(PurchasedAt));

        // Act
        var response = await handler.HandleAsync(
            new PlaceOrderCommand(customerId.Value, Email, CustomerName, [gameId.Value]),
            TestContext.Current.CancellationToken
        );

        // Assert
        var line = response.Lines.Single();
        line.ShouldSatisfyAllConditions(
            () => line.GameId.ShouldBe(gameId.Value),
            () => line.ListPriceAmount.ShouldBe(20m),
            () => line.FinalPriceAmount.ShouldBe(10m),
            () => line.AppliedPromotionId.ShouldBe(activePromotion.Id)
        );
    }

    [Fact]
    public async Task HandleAsync_WhenAnyGameDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var publishedGameId = await CreatePublishedGameAsync(
            context,
            "Portal",
            20m,
            TestContext.Current.CancellationToken
        );
        var missingGameId = Guid.NewGuid();
        var handler = new PlaceOrderCommandHandler(context, new FakeTimeProvider(PurchasedAt));

        // Act
        var exception = await Should.ThrowAsync<NotFoundException>(() =>
            handler.HandleAsync(
                new PlaceOrderCommand(
                    CustomerId.Create().Value,
                    Email,
                    CustomerName,
                    [publishedGameId.Value, missingGameId]
                ),
                TestContext.Current.CancellationToken
            )
        );

        // Assert
        exception.ResourceName.ShouldBe(nameof(Game));
        exception.Lookup.ShouldBe($"identifier '{missingGameId}'");
    }

    [Fact]
    public async Task HandleAsync_WhenAnyGameIsUnpublished_ShouldThrowConflictException()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var unpublishedGameId = await CreateGameAsync(context, "Portal", 20m, TestContext.Current.CancellationToken);
        var handler = new PlaceOrderCommandHandler(context, new FakeTimeProvider(PurchasedAt));

        // Act
        var exception = await Should.ThrowAsync<ConflictException>(() =>
            handler.HandleAsync(
                new PlaceOrderCommand(CustomerId.Create().Value, Email, CustomerName, [unpublishedGameId.Value]),
                TestContext.Current.CancellationToken
            )
        );

        // Assert
        exception.ResourceName.ShouldBe(nameof(Game));
        exception.Reason.ShouldBe("game must be published to be purchased");
    }

    [Fact]
    public async Task HandleAsync_WhenCustomerAlreadyOrderedRequestedGame_ShouldThrowConflictException()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var gameId = await CreatePublishedGameAsync(context, "Portal", 20m, TestContext.Current.CancellationToken);
        var customerId = CustomerId.Create();
        await CreateOrderAsync(context, customerId, [gameId], TestContext.Current.CancellationToken);
        var handler = new PlaceOrderCommandHandler(context, new FakeTimeProvider(PurchasedAt));

        // Act
        var exception = await Should.ThrowAsync<ConflictException>(() =>
            handler.HandleAsync(
                new PlaceOrderCommand(customerId.Value, Email, CustomerName, [gameId.Value]),
                TestContext.Current.CancellationToken
            )
        );

        // Assert
        exception.ResourceName.ShouldBe(nameof(Order));
        exception.Reason.ShouldBe($"customer already ordered game '{gameId.Value}'");
    }

    [Fact]
    public async Task HandleAsync_WithDuplicateGameIdsInRequest_ShouldThrowConflictException()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var gameId = await CreatePublishedGameAsync(context, "Portal", 20m, TestContext.Current.CancellationToken);
        var handler = new PlaceOrderCommandHandler(context, new FakeTimeProvider(PurchasedAt));

        // Act
        var exception = await Should.ThrowAsync<ConflictException>(() =>
            handler.HandleAsync(
                new PlaceOrderCommand(CustomerId.Create().Value, Email, CustomerName, [gameId.Value, gameId.Value]),
                TestContext.Current.CancellationToken
            )
        );

        // Assert
        exception.ResourceName.ShouldBe(nameof(Order));
        exception.Reason.ShouldBe("cannot contain duplicate games");
    }

    [Fact]
    public async Task HandleAsync_WithMixedValidAndInvalidGames_ShouldNotPersistNewOrder()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var ownedGameId = await CreatePublishedGameAsync(context, "Portal", 20m, TestContext.Current.CancellationToken);
        var newGameId = await CreatePublishedGameAsync(
            context,
            "Half-Life",
            15m,
            TestContext.Current.CancellationToken
        );
        var customerId = CustomerId.Create();
        await CreateOrderAsync(context, customerId, [ownedGameId], TestContext.Current.CancellationToken);
        var handler = new PlaceOrderCommandHandler(context, new FakeTimeProvider(PurchasedAt));

        // Act
        await Should.ThrowAsync<ConflictException>(() =>
            handler.HandleAsync(
                new PlaceOrderCommand(customerId.Value, Email, CustomerName, [ownedGameId.Value, newGameId.Value]),
                TestContext.Current.CancellationToken
            )
        );

        // Assert
        context.ChangeTracker.Clear();
        (await context.Orders.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(1);
    }

    public async ValueTask InitializeAsync() => await _database.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static async Task<GameId> CreateGameAsync(
        CatalogDbContext context,
        string title,
        decimal priceAmount,
        CancellationToken cancellationToken
    )
    {
        var handler = new CreateGameCommandHandler(context);
        var response = await handler.HandleAsync(
            new CreateGameCommand(title, $"{title} description.", priceAmount, Currency.Usd),
            cancellationToken
        );

        return GameId.From(response.Id);
    }

    private static async Task<GameId> CreatePublishedGameAsync(
        CatalogDbContext context,
        string title,
        decimal priceAmount,
        CancellationToken cancellationToken
    )
    {
        var gameId = await CreateGameAsync(context, title, priceAmount, cancellationToken);
        var handler = new UpdateGameCommandHandler(context, new FakeTimeProvider(PurchasedAt));
        await handler.HandleAsync(
            new UpdateGameCommand(
                gameId.Value,
                title,
                $"{title} description.",
                priceAmount,
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

    private static async Task CreateOrderAsync(
        CatalogDbContext context,
        CustomerId customerId,
        IReadOnlyList<GameId> gameIds,
        CancellationToken cancellationToken
    )
    {
        var handler = new PlaceOrderCommandHandler(context, new FakeTimeProvider(PurchasedAt));
        await handler.HandleAsync(
            new PlaceOrderCommand(customerId.Value, Email, CustomerName, [.. gameIds.Select(gameId => gameId.Value)]),
            cancellationToken
        );
    }
}
