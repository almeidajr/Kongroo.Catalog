using Kongroo.BuildingBlocks.Application;

namespace Kongroo.BuildingBlocks.Contracts;

/// <summary>Published by Catalog when an order is placed; consumed by Payments.</summary>
public sealed record OrderPlacedIntegrationEvent(
    Guid OrderId,
    Guid CustomerId,
    string Email,
    string CustomerName,
    decimal Amount,
    string Currency
) : IntegrationEvent;
