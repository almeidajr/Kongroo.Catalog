using Kongroo.BuildingBlocks.Domain;

namespace Kongroo.Catalog.Domain;

public sealed class GameOwnership : Entity<GameOwnershipId>
{
    private GameOwnership() { }

    public required OwnerId OwnerId { get; init; }

    public required GameId GameId { get; init; }

    public required OrderId OrderId { get; init; }

    public required DateTimeOffset AcquiredAt { get; init; }

    public static GameOwnership AcquireFromOrder(
        OwnerId ownerId,
        GameId gameId,
        OrderId orderId,
        DateTimeOffset acquiredAt
    )
    {
        ArgumentNullException.ThrowIfNull(ownerId);
        ArgumentNullException.ThrowIfNull(gameId);
        ArgumentNullException.ThrowIfNull(orderId);

        var ownership = new GameOwnership
        {
            Id = GameOwnershipId.Create(),
            OwnerId = ownerId,
            GameId = gameId,
            OrderId = orderId,
            AcquiredAt = acquiredAt,
        };

        ownership.RaiseDomainEvent(
            new GameAcquiredDomainEvent(
                ownership.Id,
                ownership.OwnerId,
                ownership.GameId,
                ownership.OrderId,
                ownership.AcquiredAt
            )
        );

        return ownership;
    }
}
