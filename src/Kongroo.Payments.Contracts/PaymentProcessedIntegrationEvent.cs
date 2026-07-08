using Kongroo.BuildingBlocks.Application;

namespace Kongroo.Payments.Contracts;

public sealed record PaymentProcessedIntegrationEvent(
    Guid PaymentId,
    Guid OrderId,
    Guid CustomerId,
    string CustomerEmail,
    string CustomerName,
    decimal TotalAmount,
    string Currency,
    bool IsApproved,
    DateTimeOffset ProcessedAt
) : IntegrationEvent;
