using Kongroo.BuildingBlocks.Domain.Exceptions;
using Kongroo.Catalog.Application;
using Kongroo.Catalog.Domain;
using Kongroo.Catalog.Infrastructure;
using Kongroo.Catalog.IntegrationTests.Fixtures;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace Kongroo.Catalog.IntegrationTests.Catalog.Application;

public sealed class GetGameReviewsQueryHandlerTests(PostgreSqlFixture postgreSqlFixture, MongoDbFixture mongoDbFixture)
    : IClassFixture<PostgreSqlFixture>,
        IClassFixture<MongoDbFixture>,
        IAsyncLifetime
{
    private static readonly DateTimeOffset FirstAt = new(2026, 9, 10, 12, 0, 0, TimeSpan.Zero);
    private readonly CatalogTestDatabase _database = new(postgreSqlFixture);
    private readonly ReviewsTestCollection _reviews = new(mongoDbFixture);

    [Fact]
    public async Task HandleAsync_WithTwoReviews_ShouldReturnAverageCountAndNewestFirst()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var gameId = await CreateGameAsync(context, TestContext.Current.CancellationToken);
        await SubmitAsync(context, gameId, "Ada Lovelace", 5, FirstAt, TestContext.Current.CancellationToken);
        await SubmitAsync(
            context,
            gameId,
            "Grace Hopper",
            2,
            FirstAt.AddMinutes(5),
            TestContext.Current.CancellationToken
        );
        var handler = new GetGameReviewsQueryHandler(context, _reviews.Collection);

        // Act
        var response = await handler.HandleAsync(
            new GetGameReviewsQuery(gameId.Value),
            TestContext.Current.CancellationToken
        );

        // Assert
        response.ShouldSatisfyAllConditions(
            () => response.GameId.ShouldBe(gameId.Value),
            () => response.Count.ShouldBe(2),
            () => response.AverageRating.ShouldBe(3.5),
            () => response.Reviews.Count.ShouldBe(2),
            () => response.Reviews[0].CustomerName.ShouldBe("Grace Hopper"),
            () => response.Reviews[1].CustomerName.ShouldBe("Ada Lovelace")
        );
    }

    [Fact]
    public async Task HandleAsync_WithNoReviews_ShouldReturnEmptySummary()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var gameId = await CreateGameAsync(context, TestContext.Current.CancellationToken);
        var handler = new GetGameReviewsQueryHandler(context, _reviews.Collection);

        // Act
        var response = await handler.HandleAsync(
            new GetGameReviewsQuery(gameId.Value),
            TestContext.Current.CancellationToken
        );

        // Assert
        response.ShouldSatisfyAllConditions(
            () => response.Count.ShouldBe(0),
            () => response.AverageRating.ShouldBeNull(),
            () => response.Reviews.ShouldBeEmpty()
        );
    }

    [Fact]
    public async Task HandleAsync_WhenGameDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var missingGameId = Guid.NewGuid();
        var handler = new GetGameReviewsQueryHandler(context, _reviews.Collection);

        // Act
        var exception = await Should.ThrowAsync<NotFoundException>(() =>
            handler.HandleAsync(new GetGameReviewsQuery(missingGameId), TestContext.Current.CancellationToken)
        );

        // Assert
        exception.ResourceName.ShouldBe(nameof(Game));
    }

    public async ValueTask InitializeAsync()
    {
        await _database.ResetAsync(TestContext.Current.CancellationToken);
        await _reviews.ResetAsync(TestContext.Current.CancellationToken);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private async Task SubmitAsync(
        CatalogDbContext context,
        GameId gameId,
        string customerName,
        int rating,
        DateTimeOffset submittedAt,
        CancellationToken cancellationToken
    )
    {
        var handler = new SubmitReviewCommandHandler(context, _reviews.Collection, new FakeTimeProvider(submittedAt));
        await handler.HandleAsync(
            new SubmitReviewCommand(gameId.Value, Guid.NewGuid(), customerName, rating, null),
            cancellationToken
        );
    }

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
