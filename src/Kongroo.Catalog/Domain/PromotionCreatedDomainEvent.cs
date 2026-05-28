using Kongroo.BuildingBlocks.Domain;

namespace Kongroo.Catalog.Domain;

public record PromotionCreatedDomainEvent(
    GameId GameId,
    PromotionId PromotionId,
    Percentage Discount,
    DateTimeRange ActiveRange
) : DomainEvent;

