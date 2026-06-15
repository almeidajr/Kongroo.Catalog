namespace Kongroo.Catalog.Domain;

public record OwnershipId(Guid Value)
{
    public static OwnershipId Create() => new(Guid.CreateVersion7());

    public static OwnershipId From(Guid value) => new(value);
}
