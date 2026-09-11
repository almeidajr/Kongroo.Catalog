namespace Kongroo.Catalog.Application;

/// <summary>Cache keys and the single invalidation tag for game reads.</summary>
internal static class GamesCache
{
    // ponytail: every game/promotion write evicts the whole tag. Move to per-game keys
    // (RemoveAsync(GameKey(id)) + RemoveAsync(ListKey)) if catalog write volume ever matters.
    public const string Tag = "games";
    public const string ListKey = "games:all";

    public static string GameKey(Guid gameId) => $"games:{gameId}";
}
