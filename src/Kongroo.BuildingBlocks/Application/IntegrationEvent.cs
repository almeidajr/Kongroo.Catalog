namespace Kongroo.BuildingBlocks.Application;

public abstract record IntegrationEvent
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
