using Kongroo.BuildingBlocks.Application;

namespace Kongroo.Catalog.Contracts;

public sealed record OrderPlacedLine(Guid GameId, decimal UnitPrice);

public sealed record OrderPlacedIntegrationEvent(
    Guid OrderId,
    Guid CustomerId,
    string CustomerEmail,
    string CustomerName,
    decimal TotalAmount,
    string Currency,
    IReadOnlyList<OrderPlacedLine> Lines
) : IntegrationEvent;
