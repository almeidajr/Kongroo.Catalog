using Kongroo.BuildingBlocks.Application;

namespace Kongroo.Payments.Contracts;

public sealed record PaymentProcessedIntegrationEvent(
    Guid OrderId,
    Guid UserId,
    string Email,
    string CustomerName,
    decimal Amount,
    string Currency,
    bool IsApproved,
    DateTimeOffset ProcessedAt
) : IntegrationEvent;
