using Kongroo.BuildingBlocks.Domain.Exceptions;
using Kongroo.Catalog.Domain;
using Kongroo.Catalog.Infrastructure;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

namespace Kongroo.Catalog.Application;

public sealed class GetGameReviewsQueryHandler(CatalogDbContext context, IMongoCollection<ReviewDocument> reviews)
{
    public const int PageSize = 100;

    public async Task<GetGameReviewsResponse> HandleAsync(
        GetGameReviewsQuery query,
        CancellationToken cancellationToken
    )
    {
        var gameExists = await context
            .Games.AsNoTracking()
            .AnyAsync(game => game.Id == GameId.From(query.GameId), cancellationToken);
        if (!gameExists)
        {
            throw new NotFoundException(nameof(Game), $"identifier '{query.GameId}'");
        }

        var summary = await reviews
            .Aggregate()
            .Match(review => review.GameId == query.GameId)
            .Group(
                review => review.GameId,
                group => new { Count = group.Count(), Average = group.Average(review => review.Rating) }
            )
            .FirstOrDefaultAsync(cancellationToken);

        var latest = await reviews
            .Find(review => review.GameId == query.GameId)
            .SortByDescending(review => review.CreatedAt)
            .Limit(PageSize)
            .ToListAsync(cancellationToken);

        return new GetGameReviewsResponse(
            query.GameId,
            summary is null ? null : Math.Round(summary.Average, 2),
            summary?.Count ?? 0,
            [.. latest.Select(review => review.ToResponse())]
        );
    }
}
