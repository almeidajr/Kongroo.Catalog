using Kongroo.BuildingBlocks.Application;
using Kongroo.BuildingBlocks.Contracts;
using Kongroo.Catalog.Domain;

namespace Kongroo.Catalog.Application;

public sealed class OrderPlacedDomainEventHandler(IEventBus eventBus) : DomainEventHandler<OrderPlacedDomainEvent>
{
    public override async Task HandleAsync(OrderPlacedDomainEvent domainEvent, CancellationToken cancellationToken) =>
        await eventBus.PublishAsync(
            new OrderCompletedIntegrationEvent(
                domainEvent.OrderId.Value,
                domainEvent.BuyerId.Value,
                domainEvent.PurchasedAt,
                [.. domainEvent.GameIds.Select(gameId => gameId.Value)]
            ),
            cancellationToken
        );
}

