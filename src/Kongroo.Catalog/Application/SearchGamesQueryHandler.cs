using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Search;
using Kongroo.Catalog.Domain;
using Kongroo.Catalog.Infrastructure;

namespace Kongroo.Catalog.Application;

/// <summary>Runs Game Search: published games only, typo-tolerant, best match first.</summary>
public sealed class SearchGamesQueryHandler(ElasticsearchClient client)
{
    private static readonly Fields SearchedFields = new[] { "title^3", "description" };

    public async Task<IReadOnlyList<SearchGameHitResponse>> HandleAsync(
        SearchGamesQuery query,
        CancellationToken cancellationToken
    )
    {
        var response = await client.SearchAsync<GameSearchDocument>(
            search =>
                search
                    .Indices(GameSearchDocument.IndexName)
                    .Size(query.Limit)
                    .Query(root =>
                        root.Bool(boolean =>
                            boolean
                                .Must(must =>
                                    must.MultiMatch(multiMatch =>
                                        multiMatch
                                            .Query(query.Text)
                                            .Fields(SearchedFields)
                                            .Fuzziness(new Fuzziness("AUTO"))
                                    )
                                )
                                .Filter(filter =>
                                    filter.Term(term =>
                                        term.Field(document => document.Status).Value(nameof(GameStatus.Published))
                                    )
                                )
                        )
                    )
                    .Sort(sort => sort.Score(score => score.Order(SortOrder.Desc))),
            cancellationToken
        );

        if (!response.IsValidResponse)
        {
            throw new InvalidOperationException(
                $"Searching games failed: {response.DebugInformation}",
                response.ApiCallDetails.OriginalException
            );
        }

        return [.. response.Hits.Select(ToResponse)];
    }

    private static SearchGameHitResponse ToResponse(Hit<GameSearchDocument> hit)
    {
        var document = hit.Source ?? throw new InvalidOperationException($"Search hit '{hit.Id}' has no source.");

        return new SearchGameHitResponse(
            document.Id,
            document.Title,
            document.Description,
            document.PriceAmount,
            document.Currency,
            hit.Score ?? 0
        );
    }
}
