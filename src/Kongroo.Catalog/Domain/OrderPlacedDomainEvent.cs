using Kongroo.BuildingBlocks.Domain;

namespace Kongroo.Catalog.Domain;

public sealed record OrderPlacedDomainEvent(
    OrderId OrderId,
    CustomerId CustomerId,
    DateTimeOffset PurchasedAt,
    Money Total,
    IReadOnlyList<GameId> GameIds
) : DomainEvent;
