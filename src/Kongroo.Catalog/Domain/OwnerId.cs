namespace Kongroo.Catalog.Domain;

public record OwnerId(Guid Value)
{
    public static OwnerId Create() => new(Guid.CreateVersion7());

    public static OwnerId From(Guid value) => new(value);
}

