using System.ComponentModel;

namespace Kongroo.Catalog.Application;

public sealed record GetReviewResponse(
    [property: Description("Unique identifier assigned to the review.")] Guid Id,
    [property: Description("Unique identifier of the reviewed game.")] Guid GameId,
    [property: Description("Unique identifier of the reviewing customer.")] Guid CustomerId,
    [property: Description("Display name of the reviewing customer.")] string CustomerName,
    [property: Description("Rating from 1 (worst) to 5 (best).")] int Rating,
    [property: Description("Optional free-text review.")] string? Text,
    [property: Description("Instant when the review was submitted.")] DateTimeOffset CreatedAt
);
