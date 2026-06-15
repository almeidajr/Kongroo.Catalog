namespace Kongroo.Catalog.Domain;

public record CustomerId(Guid Value)
{
    public static CustomerId Create() => new(Guid.CreateVersion7());

    public static CustomerId From(Guid value) => new(value);
}
