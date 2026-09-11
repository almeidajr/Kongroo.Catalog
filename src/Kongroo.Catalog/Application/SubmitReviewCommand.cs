namespace Kongroo.Catalog.Application;

public sealed record SubmitReviewCommand(Guid GameId, Guid CustomerId, string CustomerName, int Rating, string? Text);
