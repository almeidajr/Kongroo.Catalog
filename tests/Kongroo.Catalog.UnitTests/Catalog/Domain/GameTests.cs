using Kongroo.BuildingBlocks.Domain.Exceptions;
using Kongroo.Catalog.Domain;
using Shouldly;

namespace Kongroo.Catalog.UnitTests.Catalog.Domain;

public sealed class GameTests
{
    [Fact]
    public void Create_WithValidValues_ShouldInitializeGameWithDraftStatus()
    {
        // Arrange
        var title = GameTitle.From("Portal");
        var description = GameDescription.From("A puzzle platformer.");
        var price = Money.From(19.99m, Currency.Usd);

        // Act
        var game = Game.Create(title, description, price);

        // Assert
        game.Status.ShouldBe(GameStatus.Draft);
    }

    [Fact]
    public void Create_WithValidValues_ShouldRaiseCreatedEvent()
    {
        // Arrange
        var title = GameTitle.From("Portal");
        var description = GameDescription.From("A puzzle platformer.");
        var price = Money.From(19.99m, Currency.Usd);

        // Act
        var game = Game.Create(title, description, price);

        // Assert
        var domainEvent = game.DomainEvents.Single().ShouldBeOfType<GameCreatedDomainEvent>();
        domainEvent.GameId.ShouldBe(game.Id);
    }

    [Fact]
    public void ChangeDetails_WithValidValues_ShouldUpdateTitleAndDescription()
    {
        // Arrange
        var game = CreateGame();
        var updatedTitle = GameTitle.From("Portal 2");
        var updatedDescription = GameDescription.From("A cooperative puzzle platformer.");

        // Act
        game.ChangeDetails(updatedTitle, updatedDescription);

        // Assert
        game.Title.ShouldBe(updatedTitle);
        game.Description.ShouldBe(updatedDescription);
    }

    [Fact]
    public void ChangeDetails_WithValidValues_ShouldRaiseDetailsChangedEvent()
    {
        // Arrange
        var game = CreateGame();
        var previousTitle = game.Title;
        var previousDescription = game.Description;
        var updatedTitle = GameTitle.From("Portal 2");
        var updatedDescription = GameDescription.From("A cooperative puzzle platformer.");
        game.ClearDomainEvents();

        // Act
        game.ChangeDetails(updatedTitle, updatedDescription);

        // Assert
        var domainEvent = game.DomainEvents.Single().ShouldBeOfType<GameDetailsChangedDomainEvent>();
        domainEvent.GameId.ShouldBe(game.Id);
        domainEvent.PreviousTitle.ShouldBe(previousTitle);
        domainEvent.CurrentTitle.ShouldBe(updatedTitle);
        domainEvent.PreviousDescription.ShouldBe(previousDescription);
        domainEvent.CurrentDescription.ShouldBe(updatedDescription);
    }

    [Fact]
    public void ChangeDetails_WithSameValues_ShouldNotRaiseDetailsChangedEvent()
    {
        // Arrange
        var game = CreateGame();
        game.ClearDomainEvents();

        // Act
        game.ChangeDetails(game.Title, game.Description);

        // Assert
        game.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void ChangePrice_WithValidValue_ShouldUpdatePrice()
    {
        // Arrange
        var game = CreateGame();
        var updatedPrice = Money.From(29.99m, Currency.Eur);

        // Act
        game.ChangePrice(updatedPrice);

        // Assert
        game.Price.ShouldBe(updatedPrice);
    }

    [Fact]
    public void ChangePrice_WithValidValue_ShouldRaisePriceChangedEvent()
    {
        // Arrange
        var game = CreateGame();
        var previousPrice = game.Price;
        var updatedPrice = Money.From(29.99m, Currency.Eur);
        game.ClearDomainEvents();

        // Act
        game.ChangePrice(updatedPrice);

        // Assert
        var domainEvent = game.DomainEvents.Single().ShouldBeOfType<GamePriceChangedDomainEvent>();
        domainEvent.GameId.ShouldBe(game.Id);
        domainEvent.PreviousPrice.ShouldBe(previousPrice);
        domainEvent.CurrentPrice.ShouldBe(updatedPrice);
    }

    [Fact]
    public void ChangePrice_WithSameValue_ShouldNotRaisePriceChangedEvent()
    {
        // Arrange
        var game = CreateGame();
        game.ClearDomainEvents();

        // Act
        game.ChangePrice(game.Price);

        // Assert
        game.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void ChangeStatus_WithDefinedStatus_ShouldRaiseStatusChangedEvent()
    {
        // Arrange
        var game = CreateGame();
        game.ClearDomainEvents();

        // Act
        game.ChangeStatus(GameStatus.Published);

        // Assert
        var domainEvent = game.DomainEvents.Single().ShouldBeOfType<GameStatusChangedDomainEvent>();
        domainEvent.GameId.ShouldBe(game.Id);
        domainEvent.PreviousStatus.ShouldBe(GameStatus.Draft);
        domainEvent.CurrentStatus.ShouldBe(GameStatus.Published);
    }

    [Fact]
    public void ChangeStatus_WithSameStatus_ShouldNotRaiseStatusChangedEvent()
    {
        // Arrange
        var game = CreateGame();
        game.ClearDomainEvents();

        // Act
        game.ChangeStatus(game.Status);

        // Assert
        game.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void CreatePromotion_WithNonOverlappingRange_ShouldAddPromotion()
    {
        // Arrange
        var game = CreateGame();
        var discount = Percentage.From(25m);
        var activeRange = DateTimeRange.From(
            new DateTimeOffset(2026, 3, 30, 12, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 3, 31, 12, 0, 0, TimeSpan.Zero)
        );
        game.ClearDomainEvents();

        // Act
        var promotion = game.CreatePromotion(discount, activeRange);

        // Assert
        game.Promotions.ShouldContain(promotion);
    }

    [Fact]
    public void CreatePromotion_WithNonOverlappingRange_ShouldRaisePromotionCreatedEvent()
    {
        // Arrange
        var game = CreateGame();
        var discount = Percentage.From(25m);
        var activeRange = DateTimeRange.From(
            new DateTimeOffset(2026, 3, 30, 12, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 3, 31, 12, 0, 0, TimeSpan.Zero)
        );
        game.ClearDomainEvents();

        // Act
        var promotion = game.CreatePromotion(discount, activeRange);

        // Assert
        var domainEvent = game.DomainEvents.Single().ShouldBeOfType<PromotionCreatedDomainEvent>();
        domainEvent.ShouldSatisfyAllConditions(
            () => domainEvent.GameId.ShouldBe(game.Id),
            () => domainEvent.PromotionId.ShouldBe(promotion.Id),
            () => domainEvent.Discount.ShouldBe(discount),
            () => domainEvent.ActiveRange.ShouldBe(activeRange)
        );
    }

    [Fact]
    public void CreatePromotion_WhenRangeOverlapsExistingPromotion_ShouldThrowConflictException()
    {
        // Arrange
        var game = CreateGame();
        game.CreatePromotion(
            Percentage.From(10m),
            DateTimeRange.From(
                new DateTimeOffset(2026, 3, 30, 12, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 3, 31, 12, 0, 0, TimeSpan.Zero)
            )
        );

        // Act
        var exception = Should.Throw<ConflictException>(() =>
            game.CreatePromotion(
                Percentage.From(15m),
                DateTimeRange.From(
                    new DateTimeOffset(2026, 3, 30, 18, 0, 0, TimeSpan.Zero),
                    new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero)
                )
            )
        );

        // Assert
        exception.ResourceName.ShouldBe(nameof(Promotion));
        exception.Reason.ShouldBe("active range overlaps with an existing promotion");
    }

    [Fact]
    public void CreatePromotion_WhenRangeIsAdjacentToExistingPromotion_ShouldAllowPromotion()
    {
        // Arrange
        var game = CreateGame();
        var firstRange = DateTimeRange.From(
            new DateTimeOffset(2026, 3, 30, 12, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 3, 31, 12, 0, 0, TimeSpan.Zero)
        );
        var secondRange = DateTimeRange.From(firstRange.End, firstRange.End.AddDays(1));

        game.CreatePromotion(Percentage.From(10m), firstRange);

        // Act
        var promotion = game.CreatePromotion(Percentage.From(15m), secondRange);

        // Assert
        game.Promotions.ShouldContain(promotion);
        game.Promotions.Count.ShouldBe(2);
    }

    [Fact]
    public void QuotePurchase_WithPublishedGameAndNoActivePromotion_ShouldReturnListPrice()
    {
        // Arrange
        var game = CreateGame();
        game.ChangeStatus(GameStatus.Published);
        var asOf = new DateTimeOffset(2026, 3, 30, 12, 0, 0, TimeSpan.Zero);

        // Act
        var quote = game.QuotePurchase(asOf);

        // Assert
        quote.ShouldSatisfyAllConditions(
            () => quote.GameId.ShouldBe(game.Id),
            () => quote.GameTitle.ShouldBe(game.Title),
            () => quote.ListPrice.ShouldBe(game.Price),
            () => quote.FinalPrice.ShouldBe(game.Price),
            () => quote.AppliedPromotionId.ShouldBeNull()
        );
    }

    [Fact]
    public void QuotePurchase_WithPublishedGameAndActivePromotion_ShouldReturnDiscountedPrice()
    {
        // Arrange
        var game = Game.Create(
            GameTitle.From("Portal"),
            GameDescription.From("A puzzle platformer."),
            Money.From(20m, Currency.Usd)
        );
        game.ChangeStatus(GameStatus.Published);

        var asOf = new DateTimeOffset(2026, 3, 30, 12, 0, 0, TimeSpan.Zero);
        var promotion = game.CreatePromotion(
            Percentage.From(25m),
            DateTimeRange.From(asOf.AddHours(-1), asOf.AddHours(1))
        );

        // Act
        var quote = game.QuotePurchase(asOf);

        // Assert
        quote.ShouldSatisfyAllConditions(
            () => quote.ListPrice.ShouldBe(Money.From(20m, Currency.Usd)),
            () => quote.FinalPrice.ShouldBe(Money.From(15m, Currency.Usd)),
            () => quote.AppliedPromotionId.ShouldBe(promotion.Id)
        );
    }

    [Fact]
    public void QuotePurchase_WithPublishedGameAndInactivePromotion_ShouldIgnorePromotion()
    {
        // Arrange
        var game = CreateGame();
        game.ChangeStatus(GameStatus.Published);
        var asOf = new DateTimeOffset(2026, 3, 30, 12, 0, 0, TimeSpan.Zero);
        game.CreatePromotion(Percentage.From(25m), DateTimeRange.From(asOf.AddHours(1), asOf.AddHours(3)));

        // Act
        var quote = game.QuotePurchase(asOf);

        // Assert
        quote.FinalPrice.ShouldBe(game.Price);
        quote.AppliedPromotionId.ShouldBeNull();
    }

    [Fact]
    public void QuotePurchase_WhenGameIsNotPublished_ShouldThrowConflictException()
    {
        // Arrange
        var game = CreateGame();

        // Act
        var exception = Should.Throw<ConflictException>(() =>
            game.QuotePurchase(new DateTimeOffset(2026, 3, 30, 12, 0, 0, TimeSpan.Zero))
        );

        // Assert
        exception.ResourceName.ShouldBe(nameof(Game));
        exception.Reason.ShouldBe("game must be published to be purchased");
    }

    private static Game CreateGame() =>
        Game.Create(
            GameTitle.From("Portal"),
            GameDescription.From("A puzzle platformer."),
            Money.From(19.99m, Currency.Usd)
        );
}
