namespace Kongroo.Catalog.Infrastructure;

/// <summary>A game as Game Search indexes it.</summary>
public sealed record GameSearchDocument(
    Guid Id,
    string Title,
    string Description,
    string Status,
    decimal PriceAmount,
    string Currency
)
{
    public const string IndexName = "games";
}
