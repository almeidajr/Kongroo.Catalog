using Kongroo.BuildingBlocks.Domain;

namespace Kongroo.Catalog.Domain;

public sealed record OrderPlacedDomainEvent(
    OrderId OrderId,
    BuyerId BuyerId,
    DateTimeOffset PurchasedAt,
    Money Total,
    IReadOnlyList<GameId> GameIds
) : DomainEvent;

