using Kongroo.BuildingBlocks.Domain;

namespace Kongroo.Catalog.Domain;

public record GameCreatedDomainEvent(GameId GameId) : DomainEvent;

