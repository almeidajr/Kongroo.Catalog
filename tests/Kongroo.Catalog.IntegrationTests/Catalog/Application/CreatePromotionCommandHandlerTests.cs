using Kongroo.BuildingBlocks.Domain.Exceptions;
using Kongroo.Catalog.Application;
using Kongroo.Catalog.Domain;
using Kongroo.Catalog.Infrastructure;
using Kongroo.Catalog.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Kongroo.Catalog.IntegrationTests.Catalog.Application;

public sealed class CreatePromotionCommandHandlerTests(PostgreSqlFixture postgreSqlFixture)
    : IClassFixture<PostgreSqlFixture>,
        IAsyncLifetime
{
    private readonly CatalogTestDatabase _database = new(postgreSqlFixture);

    [Fact]
    public async Task HandleAsync_WithExistingGame_ShouldReturnPromotionResponse()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var gameId = await CreateGameAsync(context, TestContext.Current.CancellationToken);
        var handler = new CreatePromotionCommandHandler(context);
        var startsAt = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero);
        var endsAt = new DateTimeOffset(2026, 4, 15, 0, 0, 0, TimeSpan.Zero);

        // Act
        var response = await handler.HandleAsync(
            new CreatePromotionCommand(gameId.Value, 25m, startsAt, endsAt),
            TestContext.Current.CancellationToken
        );

        // Assert
        response.ShouldSatisfyAllConditions(
            () => response.Id.ShouldNotBe(Guid.Empty),
            () => response.GameId.ShouldBe(gameId.Value),
            () => response.Discount.ShouldBe(25m),
            () => response.StartsAt.ShouldBe(startsAt),
            () => response.EndsAt.ShouldBe(endsAt)
        );
    }

    [Fact]
    public async Task HandleAsync_WithExistingGame_ShouldPersistPromotion()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var gameId = await CreateGameAsync(context, TestContext.Current.CancellationToken);
        var handler = new CreatePromotionCommandHandler(context);
        var startsAt = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero);
        var endsAt = new DateTimeOffset(2026, 4, 15, 0, 0, 0, TimeSpan.Zero);

        // Act
        var response = await handler.HandleAsync(
            new CreatePromotionCommand(gameId.Value, 25m, startsAt, endsAt),
            TestContext.Current.CancellationToken
        );

        // Assert
        context.ChangeTracker.Clear();
        var savedGame = await context
            .Games.Include(candidate => candidate.Promotions)
            .SingleAsync(candidate => candidate.Id == gameId, TestContext.Current.CancellationToken);
        var savedPromotion = savedGame.Promotions.Single();

        savedPromotion.ShouldSatisfyAllConditions(
            () => savedPromotion.Id.Value.ShouldBe(response.Id),
            () => savedPromotion.Discount.Value.ShouldBe(25m),
            () => savedPromotion.ActiveRange.Start.ShouldBe(startsAt),
            () => savedPromotion.ActiveRange.End.ShouldBe(endsAt)
        );
    }

    [Fact]
    public async Task HandleAsync_WhenGameDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var missingGameId = Guid.NewGuid();
        var handler = new CreatePromotionCommandHandler(context);

        // Act
        var exception = await Should.ThrowAsync<NotFoundException>(() =>
            handler.HandleAsync(
                new CreatePromotionCommand(
                    missingGameId,
                    25m,
                    new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero),
                    new DateTimeOffset(2026, 4, 15, 0, 0, 0, TimeSpan.Zero)
                ),
                TestContext.Current.CancellationToken
            )
        );

        // Assert
        exception.ResourceName.ShouldBe(nameof(Game));
        exception.Lookup.ShouldBe($"identifier '{missingGameId}'");
    }

    [Fact]
    public async Task HandleAsync_WithOverlappingPromotion_ShouldThrowConflictException()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var gameId = await CreateGameAsync(context, TestContext.Current.CancellationToken);
        await CreatePromotionAsync(
            context,
            gameId,
            10m,
            new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 10, 0, 0, 0, TimeSpan.Zero),
            TestContext.Current.CancellationToken
        );
        var handler = new CreatePromotionCommandHandler(context);

        // Act
        var exception = await Should.ThrowAsync<ConflictException>(() =>
            handler.HandleAsync(
                new CreatePromotionCommand(
                    gameId.Value,
                    25m,
                    new DateTimeOffset(2026, 4, 5, 0, 0, 0, TimeSpan.Zero),
                    new DateTimeOffset(2026, 4, 15, 0, 0, 0, TimeSpan.Zero)
                ),
                TestContext.Current.CancellationToken
            )
        );

        // Assert
        exception.ResourceName.ShouldBe(nameof(Promotion));
        exception.Reason.ShouldBe("active range overlaps with an existing promotion");
    }

    public async ValueTask InitializeAsync() => await _database.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static async Task<GameId> CreateGameAsync(CatalogDbContext context, CancellationToken cancellationToken)
    {
        var handler = new CreateGameCommandHandler(context);
        var response = await handler.HandleAsync(
            new CreateGameCommand("Portal", "A puzzle platformer.", 19.99m, Currency.Usd),
            cancellationToken
        );

        return GameId.From(response.Id);
    }

    private static async Task<GetPromotionResponse> CreatePromotionAsync(
        CatalogDbContext context,
        GameId gameId,
        decimal discount,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        CancellationToken cancellationToken
    )
    {
        var handler = new CreatePromotionCommandHandler(context);
        return await handler.HandleAsync(
            new CreatePromotionCommand(gameId.Value, discount, startsAt, endsAt),
            cancellationToken
        );
    }
}

