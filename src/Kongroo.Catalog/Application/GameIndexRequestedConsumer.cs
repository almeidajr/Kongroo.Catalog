using Elastic.Clients.Elasticsearch;
using Elastic.Transport.Products.Elasticsearch;
using Kongroo.Catalog.Domain;
using Kongroo.Catalog.Infrastructure;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Polly;

namespace Kongroo.Catalog.Application;

/// <summary>Brings a game's Game Search document in line with Postgres: upserted while the game exists, deleted once it is gone.</summary>
public sealed class GameIndexRequestedConsumer(
    CatalogDbContext catalogContext,
    ElasticsearchClient client,
    [FromKeyedServices(GameSearchPipelines.Indexing)] ResiliencePipeline pipeline
) : IConsumer<GameIndexRequested>
{
    public async Task Consume(ConsumeContext<GameIndexRequested> context) =>
        await pipeline.ExecuteAsync(SyncAsync, context.Message.GameId, context.CancellationToken);

    // Reloaded on every attempt, so a retry never writes an older snapshot over a newer message's document.
    private async ValueTask SyncAsync(Guid gameId, CancellationToken cancellationToken)
    {
        var game = await catalogContext
            .Games.AsNoTracking()
            .SingleOrDefaultAsync(game => game.Id == GameId.From(gameId), cancellationToken);

        await (game is null ? DeleteAsync(gameId, cancellationToken) : UpsertAsync(game, cancellationToken));
    }

    private async Task DeleteAsync(Guid gameId, CancellationToken cancellationToken)
    {
        var response = await client.DeleteAsync(GameSearchDocument.IndexName, gameId, cancellationToken);

        if (!response.IsValidResponse && response.ApiCallDetails.HttpStatusCode != StatusCodes.Status404NotFound)
        {
            throw Failure(gameId, response);
        }
    }

    private async Task UpsertAsync(Game game, CancellationToken cancellationToken)
    {
        // Indexing into a missing index would auto-create it with a dynamic mapping; only GameSearchIndexInitializer creates it.
        var existsResponse = await client.Indices.ExistsAsync(GameSearchDocument.IndexName, cancellationToken);
        if (!existsResponse.Exists)
        {
            throw new InvalidOperationException(
                $"Indexing game '{game.Id.Value}' waits for the '{GameSearchDocument.IndexName}' index: {existsResponse.DebugInformation}",
                existsResponse.ApiCallDetails.OriginalException
            );
        }

        var response = await client.IndexAsync(
            GameSearchDocument.From(game),
            index => index.Index(GameSearchDocument.IndexName).Id(game.Id.Value),
            cancellationToken
        );

        if (!response.IsValidResponse)
        {
            throw Failure(game.Id.Value, response);
        }
    }

    private static InvalidOperationException Failure(Guid gameId, ElasticsearchResponse response) =>
        new($"Indexing game '{gameId}' failed: {response.DebugInformation}", response.ApiCallDetails.OriginalException);
}
