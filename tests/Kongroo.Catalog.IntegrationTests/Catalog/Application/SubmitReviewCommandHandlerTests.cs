using Kongroo.BuildingBlocks.Domain.Exceptions;
using Kongroo.Catalog.Application;
using Kongroo.Catalog.Domain;
using Kongroo.Catalog.Infrastructure;
using Kongroo.Catalog.IntegrationTests.Fixtures;
using Microsoft.Extensions.Time.Testing;
using MongoDB.Driver;
using Shouldly;

namespace Kongroo.Catalog.IntegrationTests.Catalog.Application;

public sealed class SubmitReviewCommandHandlerTests(PostgreSqlFixture postgreSqlFixture, MongoDbFixture mongoDbFixture)
    : IClassFixture<PostgreSqlFixture>,
        IClassFixture<MongoDbFixture>,
        IAsyncLifetime
{
    private static readonly DateTimeOffset SubmittedAt = new(2026, 9, 10, 12, 0, 0, TimeSpan.Zero);
    private readonly CatalogTestDatabase _database = new(postgreSqlFixture);
    private readonly ReviewsTestCollection _reviews = new(mongoDbFixture);

    [Fact]
    public async Task HandleAsync_WithExistingGame_ShouldPersistAndReturnReview()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var gameId = await CreateGameAsync(context, TestContext.Current.CancellationToken);
        var customerId = Guid.NewGuid();
        var handler = new SubmitReviewCommandHandler(context, _reviews.Collection, new FakeTimeProvider(SubmittedAt));

        // Act
        var response = await handler.HandleAsync(
            new SubmitReviewCommand(gameId.Value, customerId, "Ada Lovelace", 5, "Flawless."),
            TestContext.Current.CancellationToken
        );

        // Assert
        response.ShouldSatisfyAllConditions(
            () => response.GameId.ShouldBe(gameId.Value),
            () => response.CustomerId.ShouldBe(customerId),
            () => response.CustomerName.ShouldBe("Ada Lovelace"),
            () => response.Rating.ShouldBe(5),
            () => response.Text.ShouldBe("Flawless."),
            () => response.CreatedAt.ShouldBe(SubmittedAt)
        );

        var stored = await _reviews
            .Collection.Find(review => review.Id == response.Id)
            .SingleAsync(TestContext.Current.CancellationToken);
        stored.Rating.ShouldBe(5);
    }

    [Fact]
    public async Task HandleAsync_WhenGameDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var missingGameId = Guid.NewGuid();
        var handler = new SubmitReviewCommandHandler(context, _reviews.Collection, new FakeTimeProvider(SubmittedAt));

        // Act
        var exception = await Should.ThrowAsync<NotFoundException>(() =>
            handler.HandleAsync(
                new SubmitReviewCommand(missingGameId, Guid.NewGuid(), "Ada Lovelace", 4, null),
                TestContext.Current.CancellationToken
            )
        );

        // Assert
        exception.ResourceName.ShouldBe(nameof(Game));
        exception.Lookup.ShouldBe($"identifier '{missingGameId}'");
    }

    [Fact]
    public async Task HandleAsync_WhenCustomerAlreadyReviewedGame_ShouldThrowConflictException()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var gameId = await CreateGameAsync(context, TestContext.Current.CancellationToken);
        var customerId = Guid.NewGuid();
        var handler = new SubmitReviewCommandHandler(context, _reviews.Collection, new FakeTimeProvider(SubmittedAt));
        await handler.HandleAsync(
            new SubmitReviewCommand(gameId.Value, customerId, "Ada Lovelace", 5, null),
            TestContext.Current.CancellationToken
        );

        // Act
        var exception = await Should.ThrowAsync<ConflictException>(() =>
            handler.HandleAsync(
                new SubmitReviewCommand(gameId.Value, customerId, "Ada Lovelace", 3, "Changed my mind."),
                TestContext.Current.CancellationToken
            )
        );

        // Assert
        exception.ResourceName.ShouldBe("Review");
        exception.Reason.ShouldBe($"customer already reviewed game '{gameId.Value}'");
    }

    public async ValueTask InitializeAsync()
    {
        await _database.ResetAsync(TestContext.Current.CancellationToken);
        await _reviews.ResetAsync(TestContext.Current.CancellationToken);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static async Task<GameId> CreateGameAsync(CatalogDbContext context, CancellationToken cancellationToken)
    {
        var game = Game.Create(
            GameTitle.From("Portal"),
            GameDescription.From("A puzzle platformer."),
            Money.From(19.99m, Currency.Usd)
        );
        context.Games.Add(game);
        await context.SaveChangesAsync(cancellationToken);

        return game.Id;
    }
}
