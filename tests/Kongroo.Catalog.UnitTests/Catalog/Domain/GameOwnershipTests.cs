using Kongroo.Catalog.Domain;
using Shouldly;

namespace Kongroo.Catalog.UnitTests.Catalog.Domain;

public sealed class GameOwnershipTests
{
    [Fact]
    public void AcquireFromOrder_ShouldCreateGameOwnership()
    {
        // Arrange
        var ownerId = OwnerId.From(Guid.NewGuid());
        var gameId = GameId.From(Guid.NewGuid());
        var orderId = OrderId.From(Guid.NewGuid());
        var acquiredAt = new DateTimeOffset(2026, 3, 31, 12, 0, 0, TimeSpan.Zero);

        // Act
        var ownership = GameOwnership.AcquireFromOrder(ownerId, gameId, orderId, acquiredAt);

        // Assert
        ownership.Id.ShouldNotBeNull();
    }

    [Fact]
    public void AcquireFromOrder_ShouldStoreOwnerIdGameIdOrderIdAndAcquiredAt()
    {
        // Arrange
        var ownerId = OwnerId.From(Guid.NewGuid());
        var gameId = GameId.From(Guid.NewGuid());
        var orderId = OrderId.From(Guid.NewGuid());
        var acquiredAt = new DateTimeOffset(2026, 3, 31, 12, 0, 0, TimeSpan.Zero);

        // Act
        var ownership = GameOwnership.AcquireFromOrder(ownerId, gameId, orderId, acquiredAt);

        // Assert
        ownership.ShouldSatisfyAllConditions(
            () => ownership.OwnerId.ShouldBe(ownerId),
            () => ownership.GameId.ShouldBe(gameId),
            () => ownership.OrderId.ShouldBe(orderId),
            () => ownership.AcquiredAt.ShouldBe(acquiredAt)
        );
    }

    [Fact]
    public void AcquireFromOrder_ShouldRaiseGameAcquiredDomainEvent()
    {
        // Arrange
        var ownerId = OwnerId.From(Guid.NewGuid());
        var gameId = GameId.From(Guid.NewGuid());
        var orderId = OrderId.From(Guid.NewGuid());
        var acquiredAt = new DateTimeOffset(2026, 3, 31, 12, 0, 0, TimeSpan.Zero);

        // Act
        var ownership = GameOwnership.AcquireFromOrder(ownerId, gameId, orderId, acquiredAt);

        // Assert
        var domainEvent = ownership.DomainEvents.Single().ShouldBeOfType<GameAcquiredDomainEvent>();
        domainEvent.ShouldSatisfyAllConditions(
            () => domainEvent.GameOwnershipId.ShouldBe(ownership.Id),
            () => domainEvent.OwnerId.ShouldBe(ownerId),
            () => domainEvent.GameId.ShouldBe(gameId),
            () => domainEvent.OrderId.ShouldBe(orderId),
            () => domainEvent.AcquiredAt.ShouldBe(acquiredAt)
        );
    }
}
