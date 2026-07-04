using Kongroo.BuildingBlocks.Domain;

namespace Kongroo.Catalog.Domain;

public sealed class Ownership : AggregateRoot<OwnershipId>
{
    private Ownership() { }

    public required CustomerId CustomerId { get; init; }

    public required GameId GameId { get; init; }

    public required OrderId OrderId { get; init; }

    public required DateTimeOffset AcquiredAt { get; init; }

    public static Ownership AcquireFromOrder(
        CustomerId customerId,
        GameId gameId,
        OrderId orderId,
        DateTimeOffset acquiredAt
    )
    {
        ArgumentNullException.ThrowIfNull(customerId);
        ArgumentNullException.ThrowIfNull(gameId);
        ArgumentNullException.ThrowIfNull(orderId);

        var ownership = new Ownership
        {
            Id = OwnershipId.Create(),
            CustomerId = customerId,
            GameId = gameId,
            OrderId = orderId,
            AcquiredAt = acquiredAt,
        };

        ownership.RaiseDomainEvent(
            new GameAcquiredDomainEvent(
                ownership.Id,
                ownership.CustomerId,
                ownership.GameId,
                ownership.OrderId,
                ownership.AcquiredAt
            )
        );

        return ownership;
    }
}
