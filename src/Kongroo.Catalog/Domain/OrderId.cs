namespace Kongroo.Catalog.Domain;

public record OrderId(Guid Value)
{
    public static OrderId Create() => new(Guid.CreateVersion7());

    public static OrderId From(Guid value) => new(value);
}

