using Kongroo.BuildingBlocks.Domain;

namespace Kongroo.Catalog.Domain;

public sealed record OrderPlacedDomainEvent(
    OrderId OrderId,
    CustomerId CustomerId,
    string Email,
    string CustomerName,
    DateTimeOffset PurchasedAt,
    Money Total,
    IReadOnlyList<GameId> GameIds
) : DomainEvent;
