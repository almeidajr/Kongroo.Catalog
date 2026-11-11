namespace Kongroo.Catalog.Infrastructure;

/// <summary>Names of the resilience pipelines that keep Game Search calls going through Elasticsearch outages.</summary>
public static class GameSearchPipelines
{
    public const string Indexing = "game-search-indexing";

    public const string IndexSetup = "game-search-index-setup";
}
