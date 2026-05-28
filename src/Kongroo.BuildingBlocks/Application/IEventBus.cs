namespace Kongroo.BuildingBlocks.Application;

public interface IEventBus
{
    Task PublishAsync<TIntegrationEvent>(TIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        where TIntegrationEvent : IntegrationEvent;
}
