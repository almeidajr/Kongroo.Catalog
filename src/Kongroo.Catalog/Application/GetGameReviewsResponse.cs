using System.ComponentModel;

namespace Kongroo.Catalog.Application;

public sealed record GetGameReviewsResponse(
    [property: Description("Unique identifier of the reviewed game.")] Guid GameId,
    [property: Description("Average rating across all reviews, or null when there are none.")] double? AverageRating,
    [property: Description("Total number of reviews for the game.")] int Count,
    [property: Description("Most recent reviews, newest first (at most 100).")] IReadOnlyList<GetReviewResponse> Reviews
);
