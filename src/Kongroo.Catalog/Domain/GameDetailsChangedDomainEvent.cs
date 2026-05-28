using Kongroo.BuildingBlocks.Domain;

namespace Kongroo.Catalog.Domain;

public record GameDetailsChangedDomainEvent(
    GameId GameId,
    GameTitle PreviousTitle,
    GameTitle CurrentTitle,
    GameDescription PreviousDescription,
    GameDescription CurrentDescription
) : DomainEvent;

