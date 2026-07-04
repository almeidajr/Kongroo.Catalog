using Kongroo.BuildingBlocks.Domain;

namespace Kongroo.Catalog.Domain;

public record PromotionId(Guid Value) : IGuidId<PromotionId>
{
    public static PromotionId Create() => new(Guid.CreateVersion7());

    public static PromotionId From(Guid value) => new(value);
}
