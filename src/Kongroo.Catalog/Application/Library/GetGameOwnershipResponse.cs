using System.ComponentModel;

namespace Kongroo.Catalog.Application;

public sealed record GetGameOwnershipResponse(
    [property: Description("Unique identifier assigned to the ownership record.")] Guid Id,
    [property: Description("Unique identifier of the owner that holds the game entitlement.")] Guid OwnerId,
    [property: Description("Unique identifier of the owned game.")] Guid GameId,
    [property: Description("Unique identifier of the order that granted the ownership.")] Guid OrderId,
    [property: Description("Instant when the ownership was acquired.")] DateTimeOffset AcquiredAt
);

