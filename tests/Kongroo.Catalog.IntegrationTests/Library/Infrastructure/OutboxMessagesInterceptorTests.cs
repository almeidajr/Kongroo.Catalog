using Kongroo.Catalog.IntegrationTests.Fixtures;
using Kongroo.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Kongroo.Catalog.IntegrationTests.Library.Infrastructure;

public sealed class OutboxMessagesInterceptorTests(PostgreSqlFixture postgreSqlFixture)
    : IClassFixture<PostgreSqlFixture>,
        IAsyncLifetime
{
    private readonly LibraryTestDatabase _database = new(postgreSqlFixture);

    [Fact]
    public async Task SaveChangesAsync_WithRaisedDomainEvents_ShouldPersistOutboxMessage()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var ownership = CreateOwnership();
        context.GameOwnerships.Add(ownership);

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

        context.GameOwnerships.Add(ownership);

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

        context.GameOwnerships.Add(ownership);

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

    private static GameOwnership CreateOwnership() =>
        GameOwnership.AcquireFromOrder(OwnerId.Create(), GameId.Create(), OrderId.Create(), DateTimeOffset.UtcNow);
}

