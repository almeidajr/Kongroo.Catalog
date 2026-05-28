using Kongroo.BuildingBlocks.Domain.Exceptions;
using Kongroo.Catalog.Domain;
using Shouldly;

namespace Kongroo.Catalog.UnitTests.Catalog.Domain;

public sealed class OrderTests
{
    [Fact]
    public void PlaceCompleted_WithSingleQuote_ShouldCreateCompletedOrder()
    {
        // Arrange
        var buyerId = BuyerId.From(Guid.CreateVersion7());
        var quote = CreateQuote();
        var purchasedAt = new DateTimeOffset(2026, 3, 30, 12, 0, 0, TimeSpan.Zero);

        // Act
        var order = Order.PlaceCompleted(buyerId, [quote], purchasedAt);

        // Assert
        order.ShouldSatisfyAllConditions(
            () => order.BuyerId.ShouldBe(buyerId),
            () => order.PurchasedAt.ShouldBe(purchasedAt),
            () => order.Total.ShouldBe(quote.FinalPrice),
            () => order.Lines.Count.ShouldBe(1)
        );
    }

    [Fact]
    public void PlaceCompleted_WithSingleQuote_ShouldRaisePlacedEvent()
    {
        // Arrange
        var buyerId = BuyerId.From(Guid.CreateVersion7());
        var quote = CreateQuote();
        var purchasedAt = new DateTimeOffset(2026, 3, 30, 12, 0, 0, TimeSpan.Zero);

        // Act
        var order = Order.PlaceCompleted(buyerId, [quote], purchasedAt);

        // Assert
        var domainEvent = order.DomainEvents.Single().ShouldBeOfType<OrderPlacedDomainEvent>();
        domainEvent.ShouldSatisfyAllConditions(
            () => domainEvent.OrderId.ShouldBe(order.Id),
            () => domainEvent.BuyerId.ShouldBe(buyerId),
            () => domainEvent.PurchasedAt.ShouldBe(purchasedAt),
            () => domainEvent.Total.ShouldBe(order.Total)
        );
        domainEvent.GameIds.ShouldBe([quote.GameId]);
    }

    [Fact]
    public void PlaceCompleted_WithMultipleQuotes_ShouldComputeTotalFromFinalPrices()
    {
        // Arrange
        var buyerId = BuyerId.From(Guid.CreateVersion7());
        var firstQuote = CreateQuote(
            gameId: GameId.From(Guid.Parse("11111111-1111-1111-1111-111111111111")),
            title: "Portal",
            listPrice: 20m,
            finalPrice: 15m
        );
        var secondQuote = CreateQuote(
            gameId: GameId.From(Guid.Parse("22222222-2222-2222-2222-222222222222")),
            title: "Celeste",
            listPrice: 18m,
            finalPrice: 18m
        );

        // Act
        var order = Order.PlaceCompleted(
            buyerId,
            [firstQuote, secondQuote],
            new DateTimeOffset(2026, 3, 30, 12, 0, 0, TimeSpan.Zero)
        );

        // Assert
        order.Total.ShouldBe(Money.From(33m, Currency.Usd));
    }

    [Fact]
    public void PlaceCompleted_WithPromotionQuote_ShouldSnapshotListPriceFinalPriceAndPromotion()
    {
        // Arrange
        var buyerId = BuyerId.From(Guid.CreateVersion7());
        var promotionId = PromotionId.From(Guid.Parse("33333333-3333-3333-3333-333333333333"));
        var quote = CreateQuote(listPrice: 20m, finalPrice: 15m, appliedPromotionId: promotionId);

        // Act
        var order = Order.PlaceCompleted(buyerId, [quote], new DateTimeOffset(2026, 3, 30, 12, 0, 0, TimeSpan.Zero));

        // Assert
        var line = order.Lines.Single();
        line.ShouldSatisfyAllConditions(
            () => line.GameId.ShouldBe(quote.GameId),
            () => line.GameTitle.ShouldBe(quote.GameTitle),
            () => line.ListPrice.ShouldBe(quote.ListPrice),
            () => line.FinalPrice.ShouldBe(quote.FinalPrice),
            () => line.AppliedPromotionId.ShouldBe(promotionId)
        );
    }

    [Fact]
    public void PlaceCompleted_WhenQuotesAreEmpty_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var buyerId = BuyerId.From(Guid.CreateVersion7());

        // Act
        var exception = Should.Throw<ArgumentOutOfRangeException>(() =>
            Order.PlaceCompleted(buyerId, [], new DateTimeOffset(2026, 3, 30, 12, 0, 0, TimeSpan.Zero))
        );

        // Assert
        exception.ParamName.ShouldBe("quotes");
    }

    [Fact]
    public void PlaceCompleted_WhenQuotesContainDuplicateGames_ShouldThrowConflictException()
    {
        // Arrange
        var buyerId = BuyerId.From(Guid.CreateVersion7());
        var gameId = GameId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var firstQuote = CreateQuote(gameId: gameId, title: "Portal", listPrice: 20m, finalPrice: 15m);
        var secondQuote = CreateQuote(gameId: gameId, title: "Portal", listPrice: 20m, finalPrice: 20m);

        // Act
        var exception = Should.Throw<ConflictException>(() =>
            Order.PlaceCompleted(
                buyerId,
                [firstQuote, secondQuote],
                new DateTimeOffset(2026, 3, 30, 12, 0, 0, TimeSpan.Zero)
            )
        );

        // Assert
        exception.ResourceName.ShouldBe(nameof(Order));
        exception.Reason.ShouldBe("cannot contain duplicate games");
    }

    [Fact]
    public void PlaceCompleted_WhenQuotesUseDifferentCurrencies_ShouldThrowConflictException()
    {
        // Arrange
        var buyerId = BuyerId.From(Guid.CreateVersion7());
        var firstQuote = CreateQuote(
            gameId: GameId.From(Guid.Parse("11111111-1111-1111-1111-111111111111")),
            title: "Portal",
            listPrice: 20m,
            finalPrice: 15m,
            currency: Currency.Usd
        );
        var secondQuote = CreateQuote(
            gameId: GameId.From(Guid.Parse("22222222-2222-2222-2222-222222222222")),
            title: "Celeste",
            listPrice: 18m,
            finalPrice: 18m,
            currency: Currency.Eur
        );

        // Act
        var exception = Should.Throw<ConflictException>(() =>
            Order.PlaceCompleted(
                buyerId,
                [firstQuote, secondQuote],
                new DateTimeOffset(2026, 3, 30, 12, 0, 0, TimeSpan.Zero)
            )
        );

        // Assert
        exception.ResourceName.ShouldBe(nameof(Order));
        exception.Reason.ShouldBe("all order lines must use the same currency");
    }

    private static GamePurchaseQuote CreateQuote(
        GameId? gameId = null,
        string title = "Portal",
        decimal listPrice = 20m,
        decimal finalPrice = 15m,
        Currency currency = Currency.Usd,
        PromotionId? appliedPromotionId = null
    ) =>
        new(
            gameId ?? GameId.From(Guid.CreateVersion7()),
            GameTitle.From(title),
            Money.From(listPrice, currency),
            Money.From(finalPrice, currency),
            appliedPromotionId
        );
}

