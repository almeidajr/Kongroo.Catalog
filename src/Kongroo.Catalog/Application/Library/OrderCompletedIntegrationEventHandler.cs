using Kongroo.BuildingBlocks.Application;
using Kongroo.BuildingBlocks.Contracts;
using Kongroo.Catalog.Domain;
using Kongroo.Catalog.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Kongroo.Catalog.Application;

public sealed class OrderCompletedIntegrationEventHandler(
    LibraryDbContext context,
    ILogger<OrderCompletedIntegrationEventHandler> logger
) : IIntegrationEventHandler<OrderCompletedIntegrationEvent>
{
    public async Task HandleAsync(OrderCompletedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        var ownerships = integrationEvent.GameIds.Select(gameId =>
            GameOwnership.AcquireFromOrder(
                OwnerId.From(integrationEvent.BuyerId),
                GameId.From(gameId),
                OrderId.From(integrationEvent.OrderId),
                integrationEvent.PurchasedAt
            )
        );

        context.GameOwnerships.AddRange(ownerships);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Synchronized {OwnershipCount} library ownership(s) for order {OrderId} and owner {OwnerId}.",
            integrationEvent.GameIds.Count,
            integrationEvent.OrderId,
            integrationEvent.BuyerId
        );
    }
}

