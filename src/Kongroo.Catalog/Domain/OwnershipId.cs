using Kongroo.BuildingBlocks.Domain;

namespace Kongroo.Catalog.Domain;

public record OwnershipId(Guid Value) : IGuidId<OwnershipId>
{
    public static OwnershipId Create() => new(Guid.CreateVersion7());

    public static OwnershipId From(Guid value) => new(value);
}
