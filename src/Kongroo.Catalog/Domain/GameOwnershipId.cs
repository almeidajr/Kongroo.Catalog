namespace Kongroo.Catalog.Domain;

public record GameOwnershipId(Guid Value)
{
    public static GameOwnershipId Create() => new(Guid.CreateVersion7());

    public static GameOwnershipId From(Guid value) => new(value);
}
