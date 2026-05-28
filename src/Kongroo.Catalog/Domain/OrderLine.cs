namespace Kongroo.Catalog.Domain;

public sealed record OrderLine
{
    private OrderLine() { }

    public required GameId GameId { get; init; }

    public required GameTitle GameTitle { get; init; }

    public required Money ListPrice { get; init; }

    public required Money FinalPrice { get; init; }

    public required PromotionId? AppliedPromotionId { get; init; }

    public static OrderLine FromQuote(GamePurchaseQuote quote)
    {
        ArgumentNullException.ThrowIfNull(quote);

        return new OrderLine
        {
            GameId = quote.GameId,
            GameTitle = quote.GameTitle,
            ListPrice = quote.ListPrice,
            FinalPrice = quote.FinalPrice,
            AppliedPromotionId = quote.AppliedPromotionId,
        };
    }
}

