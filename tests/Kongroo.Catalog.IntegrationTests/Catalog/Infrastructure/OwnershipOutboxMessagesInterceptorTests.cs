using Kongroo.Catalog.Domain;
using Kongroo.Catalog.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Kongroo.Catalog.IntegrationTests.Catalog.Infrastructure;

public sealed class OwnershipOutboxMessagesInterceptorTests(PostgreSqlFixture postgreSqlFixture)
    : IClassFixture<PostgreSqlFixture>,
        IAsyncLifetime
{
    private readonly CatalogTestDatabase _database = new(postgreSqlFixture);

    [Fact]
    public async Task SaveChangesAsync_WithRaisedDomainEvents_ShouldPersistOutboxMessage()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var ownership = CreateOwnership();
        context.Ownerships.Add(ownership);

        // Act
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        context.ChangeTracker.Clear();
        var outboxMessageCount = await context.OutboxMessages.CountAsync(TestContext.Current.CancellationToken);

        outboxMessageCount.ShouldBe(1);
    }

    [Fact]
    public async Task SaveChangesAsync_WithRaisedDomainEvents_ShouldClearDomainEvents()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var ownership = CreateOwnership();

        context.Ownerships.Add(ownership);

        // Act
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        ownership.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public async Task SaveChangesAsync_WithRaisedDomainEvents_ShouldRoundTripDomainEvent()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var ownership = CreateOwnership();
        var raisedEvent = ownership.DomainEvents.Single().ShouldBeOfType<GameAcquiredDomainEvent>();

        context.Ownerships.Add(ownership);

        // Act
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        context.ChangeTracker.Clear();
        var outboxMessage = await context.OutboxMessages.SingleAsync(TestContext.Current.CancellationToken);
        var persistedEvent = outboxMessage.GetDomainEvent().ShouldBeOfType<GameAcquiredDomainEvent>();

        persistedEvent.ShouldBe(raisedEvent);
    }

    public async ValueTask InitializeAsync() => await _database.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static Ownership CreateOwnership() =>
        Ownership.AcquireFromOrder(CustomerId.Create(), GameId.Create(), OrderId.Create(), DateTimeOffset.UtcNow);
}
