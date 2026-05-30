using Kongroo.Catalog.Domain;

namespace Kongroo.Catalog.Application;

public sealed record UpdateGameCommand(
    Guid GameId,
    string Title,
    string Description,
    decimal PriceAmount,
    Currency Currency,
    GameStatus Status
);
