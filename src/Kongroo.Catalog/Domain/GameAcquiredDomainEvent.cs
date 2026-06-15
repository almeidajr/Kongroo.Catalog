using Kongroo.BuildingBlocks.Domain;

namespace Kongroo.Catalog.Domain;

public record GameAcquiredDomainEvent(
    OwnershipId OwnershipId,
    CustomerId CustomerId,
    GameId GameId,
    OrderId OrderId,
    DateTimeOffset AcquiredAt
) : DomainEvent;
