using Kongroo.BuildingBlocks.Application;
using Kongroo.Catalog.Contracts;
using Kongroo.Catalog.Domain;
using MassTransit;

namespace Kongroo.Catalog.Application;

public sealed class OrderPlacedDomainEventHandler(IPublishEndpoint publishEndpoint)
    : DomainEventHandler<OrderPlacedDomainEvent>
{
    protected override async Task HandleAsync(
        OrderPlacedDomainEvent domainEvent,
        CancellationToken cancellationToken
    ) =>
        await publishEndpoint.Publish(
            new OrderPlacedIntegrationEvent(
                domainEvent.OrderId.Value,
                domainEvent.CustomerId.Value,
                domainEvent.Email,
                domainEvent.CustomerName,
                domainEvent.Total.Amount,
                CurrencyMappings.ToCode(domainEvent.Total.Currency)
            ),
            cancellationToken
        );
}
