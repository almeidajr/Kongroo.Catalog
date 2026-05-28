using System.ComponentModel;

namespace Kongroo.Catalog.Application;

public sealed record GetPromotionResponse(
    [property: Description("Unique identifier assigned to the promotion.")] Guid Id,
    [property: Description("Unique identifier of the promoted game.")] Guid GameId,
    [property: Description("Percentage discount applied by the promotion.")] decimal Discount,
    [property: Description("Instant when the promotion becomes active.")] DateTimeOffset StartsAt,
    [property: Description("Instant when the promotion stops being active.")] DateTimeOffset EndsAt
);

