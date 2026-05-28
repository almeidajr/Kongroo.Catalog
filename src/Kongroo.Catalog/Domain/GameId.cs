namespace Kongroo.Catalog.Domain;

public record GameId(Guid Value)
{
    public static GameId Create() => new(Guid.CreateVersion7());

    public static GameId From(Guid value) => new(value);
}

