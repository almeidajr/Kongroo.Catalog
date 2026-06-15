using Kongroo.BuildingBlocks.Domain.Exceptions;
using Kongroo.Catalog.Domain;
using Shouldly;

namespace Kongroo.Catalog.UnitTests.Catalog.Domain;

public sealed class OrderTests
{
    [Fact]
    public void Place_WithSingleQuote_ShouldCreatePendingOrder()
    {
        // Arrange
        var customerId = CustomerId.From(Guid.CreateVersion7());
        var quote = CreateQuote();
        var purchasedAt = new DateTimeOffset(2026, 3, 30, 12, 0, 0, TimeSpan.Zero);

        // Act
        var order = Order.Place(customerId, [quote], purchasedAt);

        // Assert
        order.ShouldSatisfyAllConditions(
            () => order.CustomerId.ShouldBe(customerId),
            () => order.PurchasedAt.ShouldBe(purchasedAt),
            () => order.Total.ShouldBe(quote.FinalPrice),
            () => order.Lines.Count.ShouldBe(1),
            () => order.Status.ShouldBe(OrderStatus.Pending)
        );
    }

    [Fact]
    public void Place_WithSingleQuote_ShouldRaisePlacedEvent()
    {
        // Arrange
        var customerId = CustomerId.From(Guid.CreateVersion7());
        var quote = CreateQuote();
        var purchasedAt = new DateTimeOffset(2026, 3, 30, 12, 0, 0, TimeSpan.Zero);

        // Act
        var order = Order.Place(customerId, [quote], purchasedAt);

        // Assert
        var domainEvent = order.DomainEvents.Single().ShouldBeOfType<OrderPlacedDomainEvent>();
        domainEvent.ShouldSatisfyAllConditions(
            () => domainEvent.OrderId.ShouldBe(order.Id),
            () => domainEvent.CustomerId.ShouldBe(customerId),
            () => domainEvent.PurchasedAt.ShouldBe(purchasedAt),
            () => domainEvent.Total.ShouldBe(order.Total)
        );
        domainEvent.GameIds.ShouldBe([quote.GameId]);
    }

    [Fact]
    public void Place_WithMultipleQuotes_ShouldComputeTotalFromFinalPrices()
    {
        // Arrange
        var customerId = CustomerId.From(Guid.CreateVersion7());
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
        var order = Order.Place(
            customerId,
            [firstQuote, secondQuote],
            new DateTimeOffset(2026, 3, 30, 12, 0, 0, TimeSpan.Zero)
        );

        // Assert
        order.Total.ShouldBe(Money.From(33m, Currency.Usd));
    }

    [Fact]
    public void Place_WithPromotionQuote_ShouldSnapshotListPriceFinalPriceAndPromotion()
    {
        // Arrange
        var customerId = CustomerId.From(Guid.CreateVersion7());
        var promotionId = PromotionId.From(Guid.Parse("33333333-3333-3333-3333-333333333333"));
        var quote = CreateQuote(listPrice: 20m, finalPrice: 15m, appliedPromotionId: promotionId);

        // Act
        var order = Order.Place(customerId, [quote], new DateTimeOffset(2026, 3, 30, 12, 0, 0, TimeSpan.Zero));

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
    public void Place_WhenQuotesAreEmpty_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var customerId = CustomerId.From(Guid.CreateVersion7());

        // Act
        var exception = Should.Throw<ArgumentOutOfRangeException>(() =>
            Order.Place(customerId, [], new DateTimeOffset(2026, 3, 30, 12, 0, 0, TimeSpan.Zero))
        );

        // Assert
        exception.ParamName.ShouldBe("quotes");
    }

    [Fact]
    public void Place_WhenQuotesContainDuplicateGames_ShouldThrowConflictException()
    {
        // Arrange
        var customerId = CustomerId.From(Guid.CreateVersion7());
        var gameId = GameId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var firstQuote = CreateQuote(gameId: gameId, title: "Portal", listPrice: 20m, finalPrice: 15m);
        var secondQuote = CreateQuote(gameId: gameId, title: "Portal", listPrice: 20m, finalPrice: 20m);

        // Act
        var exception = Should.Throw<ConflictException>(() =>
            Order.Place(customerId, [firstQuote, secondQuote], new DateTimeOffset(2026, 3, 30, 12, 0, 0, TimeSpan.Zero))
        );

        // Assert
        exception.ResourceName.ShouldBe(nameof(Order));
        exception.Reason.ShouldBe("cannot contain duplicate games");
    }

    [Fact]
    public void Place_WhenQuotesUseDifferentCurrencies_ShouldThrowConflictException()
    {
        // Arrange
        var customerId = CustomerId.From(Guid.CreateVersion7());
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
            Order.Place(customerId, [firstQuote, secondQuote], new DateTimeOffset(2026, 3, 30, 12, 0, 0, TimeSpan.Zero))
        );

        // Assert
        exception.ResourceName.ShouldBe(nameof(Order));
        exception.Reason.ShouldBe("all order lines must use the same currency");
    }

    [Fact]
    public void MarkPaid_WhenPending_ShouldTransitionToPaid()
    {
        var order = Order.Place(
            CustomerId.From(Guid.CreateVersion7()),
            [CreateQuote()],
            new DateTimeOffset(2026, 3, 30, 12, 0, 0, TimeSpan.Zero)
        );
        var processedAt = new DateTimeOffset(2026, 3, 31, 12, 0, 0, TimeSpan.Zero);

        order.MarkPaid(processedAt);

        order.ShouldSatisfyAllConditions(
            () => order.Status.ShouldBe(OrderStatus.Paid),
            () => order.PurchasedAt.ShouldBe(processedAt)
        );
    }

    [Fact]
    public void Reject_WhenPending_ShouldTransitionToRejected()
    {
        var order = Order.Place(
            CustomerId.From(Guid.CreateVersion7()),
            [CreateQuote()],
            new DateTimeOffset(2026, 3, 30, 12, 0, 0, TimeSpan.Zero)
        );

        order.Reject();

        order.Status.ShouldBe(OrderStatus.Rejected);
    }

    [Fact]
    public void MarkPaid_WhenNotPending_ShouldThrowConflictException()
    {
        var order = Order.Place(
            CustomerId.From(Guid.CreateVersion7()),
            [CreateQuote()],
            new DateTimeOffset(2026, 3, 30, 12, 0, 0, TimeSpan.Zero)
        );
        order.Reject();

        var exception = Should.Throw<ConflictException>(() =>
            order.MarkPaid(new DateTimeOffset(2026, 3, 31, 12, 0, 0, TimeSpan.Zero))
        );

        exception.ResourceName.ShouldBe(nameof(Order));
    }

    [Fact]
    public void MarkPaid_WhenAlreadyPaid_ShouldThrowConflictException()
    {
        var order = Order.Place(
            CustomerId.From(Guid.CreateVersion7()),
            [CreateQuote()],
            new DateTimeOffset(2026, 3, 30, 12, 0, 0, TimeSpan.Zero)
        );
        order.MarkPaid(new DateTimeOffset(2026, 3, 31, 12, 0, 0, TimeSpan.Zero));

        var exception = Should.Throw<ConflictException>(() =>
            order.MarkPaid(new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero))
        );

        exception.ResourceName.ShouldBe(nameof(Order));
    }

    [Fact]
    public void Reject_WhenNotPending_ShouldThrowConflictException()
    {
        var order = Order.Place(
            CustomerId.From(Guid.CreateVersion7()),
            [CreateQuote()],
            new DateTimeOffset(2026, 3, 30, 12, 0, 0, TimeSpan.Zero)
        );
        order.MarkPaid(new DateTimeOffset(2026, 3, 31, 12, 0, 0, TimeSpan.Zero));

        var exception = Should.Throw<ConflictException>(order.Reject);

        exception.ResourceName.ShouldBe(nameof(Order));
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
