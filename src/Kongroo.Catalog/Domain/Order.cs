using Kongroo.BuildingBlocks.Domain;
using Kongroo.BuildingBlocks.Domain.Exceptions;

namespace Kongroo.Catalog.Domain;

public sealed class Order : Entity<OrderId>
{
    private readonly List<OrderLine> _lines = [];

    private Order() { }

    public BuyerId BuyerId { get; private set; } = null!;

    public DateTimeOffset PurchasedAt { get; private set; }

    public Money Total { get; private set; } = null!;

    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();

    public static Order PlaceCompleted(
        BuyerId buyerId,
        IReadOnlyList<GamePurchaseQuote> quotes,
        DateTimeOffset purchasedAt
    )
    {
        ArgumentNullException.ThrowIfNull(buyerId);
        ArgumentNullException.ThrowIfNull(quotes);

        if (quotes.Count == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quotes),
                quotes.Count,
                "Order must contain at least one quote."
            );
        }

        ThrowIfContainsDuplicateGames(quotes);
        var currency = GetCurrency(quotes);

        var order = new Order
        {
            Id = OrderId.Create(),
            BuyerId = buyerId,
            PurchasedAt = purchasedAt,
            Total = Money.From(quotes.Sum(quote => quote.FinalPrice.Amount), currency),
        };

        order._lines.AddRange(quotes.Select(OrderLine.FromQuote));

        order.RaiseDomainEvent(
            new OrderPlacedDomainEvent(
                order.Id,
                order.BuyerId,
                order.PurchasedAt,
                order.Total,
                [.. order._lines.Select(line => line.GameId)]
            )
        );

        return order;
    }

    private static void ThrowIfContainsDuplicateGames(IReadOnlyList<GamePurchaseQuote> quotes)
    {
        var hasDuplicates = quotes.GroupBy(quote => quote.GameId).Any(group => group.Count() > 1);

        if (hasDuplicates)
        {
            throw new ConflictException(nameof(Order), "cannot contain duplicate games");
        }
    }

    private static Currency GetCurrency(IReadOnlyList<GamePurchaseQuote> quotes)
    {
        var currency = quotes[0].FinalPrice.Currency;
        var hasMixedCurrency = quotes.Any(quote =>
            quote.ListPrice.Currency != quote.FinalPrice.Currency || quote.FinalPrice.Currency != currency
        );

        if (hasMixedCurrency)
        {
            throw new ConflictException(nameof(Order), "all order lines must use the same currency");
        }

        return currency;
    }
}

