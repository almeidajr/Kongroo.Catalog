using Kongroo.Catalog.Domain;
using Shouldly;

namespace Kongroo.Catalog.UnitTests.Catalog.Domain;

public sealed class OwnershipTests
{
    [Fact]
    public void AcquireFromOrder_WithOrderDetails_ShouldStoreOwnershipFieldsAndRaiseGameAcquiredDomainEvent()
    {
        // Arrange
        var customerId = CustomerId.From(Guid.NewGuid());
        var gameId = GameId.From(Guid.NewGuid());
        var orderId = OrderId.From(Guid.NewGuid());
        var acquiredAt = new DateTimeOffset(2026, 3, 31, 12, 0, 0, TimeSpan.Zero);

        // Act
        var ownership = Ownership.AcquireFromOrder(customerId, gameId, orderId, acquiredAt);

        // Assert
        ownership.ShouldSatisfyAllConditions(
            () => ownership.Id.Value.ShouldNotBe(Guid.Empty),
            () => ownership.CustomerId.ShouldBe(customerId),
            () => ownership.GameId.ShouldBe(gameId),
            () => ownership.OrderId.ShouldBe(orderId),
            () => ownership.AcquiredAt.ShouldBe(acquiredAt)
        );
        var domainEvent = ownership.DomainEvents.Single().ShouldBeOfType<GameAcquiredDomainEvent>();
        domainEvent.ShouldSatisfyAllConditions(
            () => domainEvent.OwnershipId.ShouldBe(ownership.Id),
            () => domainEvent.CustomerId.ShouldBe(customerId),
            () => domainEvent.GameId.ShouldBe(gameId),
            () => domainEvent.OrderId.ShouldBe(orderId),
            () => domainEvent.AcquiredAt.ShouldBe(acquiredAt)
        );
    }
}
