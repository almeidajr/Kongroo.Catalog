namespace Kongroo.BuildingBlocks.Infrastructure;

public record OutboxMessageId(Guid Value)
{
    public static OutboxMessageId Create() => new(Guid.CreateVersion7());

    public static OutboxMessageId From(Guid value) => new(value);
}
