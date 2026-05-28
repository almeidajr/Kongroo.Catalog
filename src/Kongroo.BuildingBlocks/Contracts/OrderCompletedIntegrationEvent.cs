using Kongroo.BuildingBlocks.Application;

namespace Kongroo.BuildingBlocks.Contracts;

public sealed record OrderCompletedIntegrationEvent(
    Guid OrderId,
    Guid BuyerId,
    DateTimeOffset PurchasedAt,
    IReadOnlyList<Guid> GameIds
) : IntegrationEvent;
