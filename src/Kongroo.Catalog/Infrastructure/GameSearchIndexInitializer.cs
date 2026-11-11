using Elastic.Clients.Elasticsearch;

namespace Kongroo.Catalog.Infrastructure;

/// <summary>Creates the Game Search index with its explicit mapping when it is missing.</summary>
public sealed class GameSearchIndexInitializer(
    ElasticsearchClient client,
    TimeProvider timeProvider,
    ILogger<GameSearchIndexInitializer> logger
) : BackgroundService
{
    private const string IndexAlreadyExists = "resource_already_exists_exception";
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!await TryCreateIndexAsync(stoppingToken))
        {
            await Task.Delay(RetryDelay, timeProvider, stoppingToken);
        }
    }

    private async Task<bool> TryCreateIndexAsync(CancellationToken cancellationToken)
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

        if (response.IsValidResponse || response.ElasticsearchServerError?.Error?.Type == IndexAlreadyExists)
        {
            return true;
        }

        // shortcut: fixed delay, no backoff; ticket 09 replaces this loop with a capped-backoff Polly pipeline.
        logger.LogWarning(
            response.ApiCallDetails.OriginalException,
            "Creating the {IndexName} index failed, retrying in {RetryDelay}: {DebugInformation}",
            GameSearchDocument.IndexName,
            RetryDelay,
            response.DebugInformation
        );

        return false;
    }
}
