using Kongroo.BuildingBlocks.Application;
using Kongroo.BuildingBlocks.Contracts;
using Kongroo.Catalog.Domain;
using MassTransit;

namespace Kongroo.Catalog.Application;

public sealed class OrderPlacedDomainEventHandler(IPublishEndpoint publishEndpoint)
    : DomainEventHandler<OrderPlacedDomainEvent>
{
    public override async Task HandleAsync(OrderPlacedDomainEvent domainEvent, CancellationToken cancellationToken) =>
        await publishEndpoint.Publish(
            new OrderPlacedIntegrationEvent(
                domainEvent.OrderId.Value,
                domainEvent.CustomerId.Value,
                domainEvent.Total.Amount,
                CurrencyMappings.ToCode(domainEvent.Total.Currency)
            ),
            cancellationToken
        );
}
