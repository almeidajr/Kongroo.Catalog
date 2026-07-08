using Kongroo.BuildingBlocks.Domain;
using Kongroo.BuildingBlocks.Domain.Exceptions;

namespace Kongroo.Catalog.Domain;

public sealed class Order : AggregateRoot<OrderId>
{
    private readonly List<OrderLine> _lines = [];

    private Order() { }

    public CustomerId CustomerId { get; private set; } = null!;

    public DateTimeOffset PurchasedAt { get; private set; }

    public Money Total { get; private set; } = null!;

    public OrderStatus Status { get; private set; }

    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();

    public static Order Place(
        CustomerId customerId,
        string email,
        string customerName,
        IReadOnlyList<GamePurchaseQuote> quotes,
        DateTimeOffset purchasedAt
    )
    {
        ArgumentNullException.ThrowIfNull(customerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(customerName);
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
            CustomerId = customerId,
            PurchasedAt = purchasedAt,
            Total = Money.From(quotes.Sum(quote => quote.FinalPrice.Amount), currency),
            Status = OrderStatus.Pending,
        };

        order._lines.AddRange(quotes.Select(OrderLine.FromQuote));

        order.RaiseDomainEvent(
            new OrderPlacedDomainEvent(
                order.Id,
                order.CustomerId,
                email,
                customerName,
                order.PurchasedAt,
                order.Total,
                [.. order._lines.Select(line => new OrderPlacedGameLine(line.GameId, line.FinalPrice))]
            )
        );

        return order;
    }

    public void MarkPaid(DateTimeOffset processedAt)
    {
        EnsurePending();
        Status = OrderStatus.Paid;
        PurchasedAt = processedAt; // advance to the confirmed payment instant
    }

    public void Reject()
    {
        EnsurePending();
        Status = OrderStatus.Rejected;
    }

    private void EnsurePending()
    {
        if (Status != OrderStatus.Pending)
        {
            throw new ConflictException(nameof(Order), $"order is already '{Status}'");
        }
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
