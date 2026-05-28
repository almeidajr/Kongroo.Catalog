namespace Kongroo.Catalog.Domain;

public record BuyerId(Guid Value)
{
    public static BuyerId Create() => new(Guid.CreateVersion7());

    public static BuyerId From(Guid value) => new(value);
}

