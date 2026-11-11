using Elastic.Clients.Elasticsearch;
using Microsoft.EntityFrameworkCore;
using Polly;

namespace Kongroo.Catalog.Infrastructure;

/// <summary>Creates the Game Search index with its explicit mapping when it is missing, and backfills it from Postgres if this replica created it.</summary>
public sealed class GameSearchIndexInitializer(
    ElasticsearchClient client,
    IServiceScopeFactory scopeFactory,
    [FromKeyedServices(GameSearchPipelines.IndexSetup)] ResiliencePipeline pipeline,
    ILogger<GameSearchIndexInitializer> logger
) : BackgroundService
{
    private const string IndexAlreadyExists = "resource_already_exists_exception";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var wasCreated = await pipeline.ExecuteAsync(CreateIndexAsync, stoppingToken);

        if (wasCreated)
        {
            await pipeline.ExecuteAsync(BackfillAsync, stoppingToken);
        }
    }

    private async ValueTask<bool> CreateIndexAsync(CancellationToken cancellationToken)
    {
        var response = await client.Indices.CreateAsync<GameSearchDocument>(
            GameSearchDocument.IndexName,
            index =>
                index.Mappings(mappings =>
                    mappings.Properties(properties =>
                        properties
                            .Keyword(document => document.Id)
                            .Text(document => document.Title, text => text.Analyzer("standard"))
                            .Text(document => document.Description, text => text.Analyzer("standard"))
                            .Keyword(document => document.Status)
                            .DoubleNumber(document => document.PriceAmount)
                            .Keyword(document => document.Currency)
                    )
                ),
            cancellationToken
        );

        if (response.IsValidResponse)
        {
            return true;
        }

        if (response.ElasticsearchServerError?.Error?.Type == IndexAlreadyExists)
        {
            return false;
        }

        throw new InvalidOperationException(
            $"Creating the '{GameSearchDocument.IndexName}' index failed: {response.DebugInformation}",
            response.ApiCallDetails.OriginalException
        );
    }

    private async ValueTask BackfillAsync(CancellationToken cancellationToken)
    {
        var documents = await LoadDocumentsAsync(cancellationToken);

        if (documents.Count > 0)
        {
            await CreateDocumentsAsync(documents, cancellationToken);
        }

        logger.LogInformation(
            "Backfilled {GameCount} games into the {IndexName} index",
            documents.Count,
            GameSearchDocument.IndexName
        );
    }

    private async Task<List<GameSearchDocument>> LoadDocumentsAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var catalogContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        // shortcut: one bulk request for every game; page it if the catalog grows past a few thousand games.
        var games = await catalogContext.Games.AsNoTracking().ToListAsync(cancellationToken);

        return [.. games.Select(GameSearchDocument.From)];
    }

    // Create, not index: a document the consumer already wrote is newer than this read and must win.
    private async Task CreateDocumentsAsync(List<GameSearchDocument> documents, CancellationToken cancellationToken)
    {
        var response = await client.BulkAsync(
            bulk =>
                bulk.Index(GameSearchDocument.IndexName)
                    .CreateMany(documents, static (operation, document) => operation.Id(document.Id)),
            cancellationToken
        );

        var hasFailed =
            !response.ApiCallDetails.HasSuccessfulStatusCode
            || response.ItemsWithErrors.Any(static item => item.Status != StatusCodes.Status409Conflict);

        if (hasFailed)
        {
            throw new InvalidOperationException(
                $"Backfilling the '{GameSearchDocument.IndexName}' index failed: {response.DebugInformation}",
                response.ApiCallDetails.OriginalException
            );
        }
    }
}
