using Kongroo.BuildingBlocks.Domain;

namespace Kongroo.Catalog.Domain;

public record GameStatusChangedDomainEvent(GameId GameId, GameStatus PreviousStatus, GameStatus CurrentStatus)
    : DomainEvent;
