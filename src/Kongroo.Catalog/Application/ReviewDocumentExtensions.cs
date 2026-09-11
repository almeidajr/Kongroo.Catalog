using Kongroo.Catalog.Infrastructure;

namespace Kongroo.Catalog.Application;

internal static class ReviewDocumentExtensions
{
    extension(ReviewDocument document)
    {
        public GetReviewResponse ToResponse() =>
            new(
                document.Id,
                document.GameId,
                document.CustomerId,
                document.CustomerName,
                document.Rating,
                document.Text,
                new DateTimeOffset(document.CreatedAt, TimeSpan.Zero)
            );
    }
}
