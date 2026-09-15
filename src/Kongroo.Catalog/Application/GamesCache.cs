namespace Kongroo.Catalog.Application;

/// <summary>Cache keys and the single invalidation tag for game reads.</summary>
public static class GamesCache
{
    public const string Tag = "games";
    public const string ListKey = "games:all";

    public static string GameKey(Guid gameId) => $"games:{gameId}";
}
