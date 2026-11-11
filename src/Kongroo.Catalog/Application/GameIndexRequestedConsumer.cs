using Elastic.Clients.Elasticsearch;
using Kongroo.Catalog.Domain;
using Kongroo.Catalog.Infrastructure;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Kongroo.Catalog.Application;

/// <summary>Upserts a game's Game Search document from its current state in Postgres.</summary>
public sealed class GameIndexRequestedConsumer(CatalogDbContext catalogContext, ElasticsearchClient client)
    : IConsumer<GameIndexRequested>
{
    public async Task Consume(ConsumeContext<GameIndexRequested> context)
    {
        var game = await catalogContext
            .Games.AsNoTracking()
            .SingleOrDefaultAsync(game => game.Id == GameId.From(context.Message.GameId), context.CancellationToken);

        if (game is null)
        {
            return;
        }

        var response = await client.IndexAsync(
            ToDocument(game),
            index => index.Index(GameSearchDocument.IndexName).Id(game.Id.Value),
            context.CancellationToken
        );

        if (!response.IsValidResponse)
        {
            throw new InvalidOperationException(
                $"Indexing game '{game.Id.Value}' failed: {response.DebugInformation}",
                response.ApiCallDetails.OriginalException
            );
        }
    }

    private static GameSearchDocument ToDocument(Game game) =>
        new(
            game.Id.Value,
            game.Title.Value,
            game.Description.Value,
            game.Status.ToString(),
            game.Price.Amount,
            CurrencyMappings.ToCode(game.Price.Currency)
        );
}
