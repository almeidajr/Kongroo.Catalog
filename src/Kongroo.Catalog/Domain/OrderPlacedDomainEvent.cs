using Kongroo.BuildingBlocks.Domain;

namespace Kongroo.Catalog.Domain;

public sealed record OrderPlacedGameLine(GameId GameId, Money Price);

public sealed record OrderPlacedDomainEvent(
    OrderId OrderId,
    CustomerId CustomerId,
    string Email,
    string CustomerName,
    DateTimeOffset PurchasedAt,
    Money Total,
    IReadOnlyList<OrderPlacedGameLine> Lines
) : DomainEvent
{
    public IReadOnlyList<GameId> GameIds => [.. Lines.Select(line => line.GameId)];
}
