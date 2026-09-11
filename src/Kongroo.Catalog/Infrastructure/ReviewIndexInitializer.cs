using Kongroo.BuildingBlocks.Application;
using MongoDB.Driver;

namespace Kongroo.Catalog.Infrastructure;

/// <summary>Creates the unique (gameId, customerId) index so a customer can review a game once.</summary>
public sealed class ReviewIndexInitializer(IMongoCollection<ReviewDocument> reviews) : IApplicationInitializer
{
    public const string IndexName = "ux_reviews_game_customer";

    public int Priority => 1;

    public ValueTask<bool> IsEnabledAsync(CancellationToken cancellationToken) => ValueTask.FromResult(true);

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var keys = Builders<ReviewDocument>
            .IndexKeys.Ascending(review => review.GameId)
            .Ascending(review => review.CustomerId);
        var model = new CreateIndexModel<ReviewDocument>(
            keys,
            new CreateIndexOptions { Unique = true, Name = IndexName }
        );

        await reviews.Indexes.CreateOneAsync(model, cancellationToken: cancellationToken);
    }
}
