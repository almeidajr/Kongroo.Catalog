using Kongroo.Catalog.Domain;
using Kongroo.Catalog.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Kongroo.Catalog.IntegrationTests.Catalog.Infrastructure;

public sealed class OutboxMessagesInterceptorTests(PostgreSqlFixture postgreSqlFixture)
    : IClassFixture<PostgreSqlFixture>,
        IAsyncLifetime
{
    private readonly CatalogTestDatabase _database = new(postgreSqlFixture);

    [Fact]
    public async Task SaveChangesAsync_WithRaisedDomainEvents_ShouldPersistOutboxMessage()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var game = CreateGame();
        context.Games.Add(game);

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
        var game = CreateGame();

        context.Games.Add(game);

        // Act
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        game.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public async Task SaveChangesAsync_WithRaisedDomainEvents_ShouldRoundTripDomainEvent()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var game = CreateGame();
        var raisedEvent = game.DomainEvents.Single().ShouldBeOfType<GameCreatedDomainEvent>();

        context.Games.Add(game);

        // Act
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        context.ChangeTracker.Clear();
        var outboxMessage = await context.OutboxMessages.SingleAsync(TestContext.Current.CancellationToken);
        var persistedEvent = outboxMessage.GetDomainEvent().ShouldBeOfType<GameCreatedDomainEvent>();

        persistedEvent.ShouldBe(raisedEvent);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenCalledWithoutNewDomainEvents_ShouldNotPersistDuplicateOutboxMessages()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var game = Game.Create(
            GameTitle.From("Portal"),
            GameDescription.From("A puzzle platformer."),
            Money.From(19.99m, Currency.Usd)
        );

        context.Games.Add(game);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        context.ChangeTracker.Clear();
        var outboxMessageCount = await context.OutboxMessages.CountAsync(TestContext.Current.CancellationToken);

        outboxMessageCount.ShouldBe(1);
    }

    public async ValueTask InitializeAsync() => await _database.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static Game CreateGame() =>
        Game.Create(
            GameTitle.From("Portal"),
            GameDescription.From("A puzzle platformer."),
            Money.From(19.99m, Currency.Usd)
        );
}

