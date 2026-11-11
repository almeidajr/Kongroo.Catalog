using Elastic.Clients.Elasticsearch;

namespace Kongroo.Catalog.Infrastructure;

/// <summary>Creates the Game Search index with its explicit mapping when it is missing.</summary>
public sealed class GameSearchIndexInitializer(ElasticsearchClient client) : BackgroundService
{
    private const string IndexAlreadyExists = "resource_already_exists_exception";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
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
            stoppingToken
        );

        if (!response.IsValidResponse && response.ElasticsearchServerError?.Error?.Type != IndexAlreadyExists)
        {
            throw new InvalidOperationException(
                $"Creating the '{GameSearchDocument.IndexName}' index failed: {response.DebugInformation}",
                response.ApiCallDetails.OriginalException
            );
        }
    }
}
