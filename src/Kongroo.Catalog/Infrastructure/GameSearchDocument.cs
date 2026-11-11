using Kongroo.Catalog.Domain;

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

    public static GameSearchDocument From(Game game) =>
        new(
            game.Id.Value,
            game.Title.Value,
            game.Description.Value,
            game.Status.ToString(),
            game.Price.Amount,
            CurrencyMappings.ToCode(game.Price.Currency)
        );
}
