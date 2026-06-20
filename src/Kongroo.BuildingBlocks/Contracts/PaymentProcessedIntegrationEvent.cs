using Kongroo.BuildingBlocks.Application;

namespace Kongroo.BuildingBlocks.Contracts;

/// <summary>Published by Payments after a payment decision; consumed by Catalog and Notifications.</summary>
public sealed record PaymentProcessedIntegrationEvent(
    Guid OrderId,
    Guid UserId,
    string Email,
    string CustomerName,
    decimal Amount,
    string Currency,
    bool Approved,
    DateTimeOffset ProcessedAt
) : IntegrationEvent;
