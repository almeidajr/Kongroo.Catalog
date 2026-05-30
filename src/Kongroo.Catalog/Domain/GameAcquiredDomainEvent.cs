using Kongroo.BuildingBlocks.Domain;

namespace Kongroo.Catalog.Domain;

public record GameAcquiredDomainEvent(
    GameOwnershipId GameOwnershipId,
    OwnerId OwnerId,
    GameId GameId,
    OrderId OrderId,
    DateTimeOffset AcquiredAt
) : DomainEvent;
