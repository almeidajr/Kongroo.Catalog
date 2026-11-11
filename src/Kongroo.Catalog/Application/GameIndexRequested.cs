namespace Kongroo.Catalog.Application;

/// <summary>Asks Catalog to bring a game's Game Search document in line with Postgres.</summary>
public sealed record GameIndexRequested(Guid GameId);
