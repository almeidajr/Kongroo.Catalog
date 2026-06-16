using Kongroo.BuildingBlocks.Application;

namespace Kongroo.BuildingBlocks.Contracts;

public sealed record OrderPlacedIntegrationEvent(Guid OrderId, Guid CustomerId, decimal Amount, string Currency)
    : IntegrationEvent;
