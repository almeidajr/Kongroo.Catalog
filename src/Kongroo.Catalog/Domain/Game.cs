using Kongroo.BuildingBlocks.Domain;
using Kongroo.BuildingBlocks.Domain.Exceptions;

namespace Kongroo.Catalog.Domain;

public sealed class Game : Entity<GameId>
{
    private readonly List<Promotion> _promotions = [];

    private Game() { }

    public GameTitle Title { get; private set; } = null!;

    public GameDescription Description { get; private set; } = null!;

    public Money Price { get; private set; } = Money.Zero;

    public GameStatus Status { get; private set; }

    public IReadOnlyCollection<Promotion> Promotions => _promotions.AsReadOnly();

    public static Game Create(GameTitle title, GameDescription description, Money price)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(description);
        ArgumentNullException.ThrowIfNull(price);

        var game = new Game
        {
            Id = GameId.Create(),
            Title = title,
            Description = description,
            Price = price,
            Status = GameStatus.Draft,
        };

        game.RaiseDomainEvent(new GameCreatedDomainEvent(game.Id));

        return game;
    }

    public void ChangeDetails(GameTitle title, GameDescription description)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(description);

        if (Title == title && Description == description)
        {
            return;
        }

        var previousTitle = Title;
        var previousDescription = Description;
        Title = title;
        Description = description;
        RaiseDomainEvent(new GameDetailsChangedDomainEvent(Id, previousTitle, Title, previousDescription, Description));
    }

    public void ChangePrice(Money price)
    {
        ArgumentNullException.ThrowIfNull(price);

        if (Price == price)
        {
            return;
        }

        var previousPrice = Price;
        Price = price;
        RaiseDomainEvent(new GamePriceChangedDomainEvent(Id, previousPrice, Price));
    }

    public void ChangeStatus(GameStatus status)
    {
        if (Status == status)
        {
            return;
        }

        var previousStatus = Status;
        Status = status;
        RaiseDomainEvent(new GameStatusChangedDomainEvent(Id, previousStatus, Status));
    }

    public Promotion CreatePromotion(Percentage discount, DateTimeRange activeRange)
    {
        ArgumentNullException.ThrowIfNull(discount);
        ArgumentNullException.ThrowIfNull(activeRange);

        if (_promotions.Any(promotion => promotion.ActiveRange.Overlaps(activeRange)))
        {
            throw new ConflictException(nameof(Promotion), "active range overlaps with an existing promotion");
        }

        var promotion = Promotion.Create(discount, activeRange);
        _promotions.Add(promotion);
        RaiseDomainEvent(new PromotionCreatedDomainEvent(Id, promotion.Id, promotion.Discount, promotion.ActiveRange));

        return promotion;
    }

    public Promotion? GetActivePromotion(DateTimeOffset asOf) =>
        _promotions.SingleOrDefault(candidate => candidate.ActiveRange.Contains(asOf));

    public GamePurchaseQuote QuotePurchase(DateTimeOffset asOf)
    {
        if (Status != GameStatus.Published)
        {
            throw new ConflictException(nameof(Game), "game must be published to be purchased");
        }

        var promotion = GetActivePromotion(asOf);
        var finalPrice = Price.ApplyDiscount(promotion?.Discount ?? Percentage.MinValue);

        return new GamePurchaseQuote(Id, Title, Price, finalPrice, promotion?.Id);
    }
}
