using Kongroo.BuildingBlocks.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kongroo.BuildingBlocks.Infrastructure;

public sealed class InProcessEventBus(ILogger<InProcessEventBus> logger, IServiceProvider serviceProvider) : IEventBus
{
    public async Task PublishAsync<TIntegrationEvent>(
        TIntegrationEvent integrationEvent,
        CancellationToken cancellationToken
    )
        where TIntegrationEvent : IntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var handlers = serviceProvider.GetServices<IIntegrationEventHandler<TIntegrationEvent>>().ToArray();

        logger.LogDebug(
            "Publishing integration event {IntegrationEventType} with id {IntegrationEventId} to {HandlerCount} handler(s).",
            typeof(TIntegrationEvent).Name,
            integrationEvent.Id,
            handlers.Length
        );

        foreach (var handler in handlers)
        {
            logger.LogTrace(
                "Dispatching integration event {IntegrationEventType} with id {IntegrationEventId} to handler {HandlerType}.",
                typeof(TIntegrationEvent).Name,
                integrationEvent.Id,
                handler.GetType().Name
            );

            await handler.HandleAsync(integrationEvent, cancellationToken);
        }

        logger.LogDebug(
            "Published integration event {IntegrationEventType} with id {IntegrationEventId}.",
            typeof(TIntegrationEvent).Name,
            integrationEvent.Id
        );
    }
}
