using Kongroo.BuildingBlocks.Application;

namespace Kongroo.Catalog.Contracts;

public sealed record OrderPlacedIntegrationEvent(
    Guid OrderId,
    Guid CustomerId,
    string Email,
    string CustomerName,
    decimal Amount,
    string Currency
) : IntegrationEvent;
