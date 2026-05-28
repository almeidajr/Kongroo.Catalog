using System.ComponentModel;
using Kongroo.Catalog.Domain;

namespace Kongroo.Catalog.Application;

public sealed record GetOrderResponse(
    [property: Description("Unique identifier assigned to the order.")] Guid Id,
    [property: Description("Unique identifier of the buyer that placed the order.")] Guid BuyerId,
    [property: Description("Instant when the order was completed.")] DateTimeOffset PurchasedAt,
    [property: Description("Total charged amount for the order.")] decimal TotalAmount,
    [property: Description("Currency used by the order total.")] Currency Currency,
    [property: Description("Snapshot of the purchased order lines.")] IReadOnlyList<GetOrderLineResponse> Lines
);

