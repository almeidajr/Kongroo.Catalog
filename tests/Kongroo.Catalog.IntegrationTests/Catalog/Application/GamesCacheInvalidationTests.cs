using Kongroo.BuildingBlocks.Domain.Exceptions;
using Kongroo.Catalog.Application;
using Kongroo.Catalog.Domain;
using Kongroo.Catalog.Infrastructure;
using Kongroo.Catalog.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace Kongroo.Catalog.IntegrationTests.Catalog.Application;

public sealed class GamesCacheInvalidationTests(PostgreSqlFixture postgreSqlFixture, RedisFixture redisFixture)
    : IClassFixture<PostgreSqlFixture>,
        IClassFixture<RedisFixture>,
        IAsyncLifetime
{
    private static readonly DateTimeOffset ReadAt = new(2026, 9, 10, 12, 0, 0, TimeSpan.Zero);
    private readonly CatalogTestDatabase _database = new(postgreSqlFixture);

    [Fact]
    public async Task HandleAsync_WhenGameChangesOutsideTheHandlers_ShouldServeTheCachedResponseFromRedis()
    {
        // Arrange — two cache instances (separate L1) sharing one Redis key space
        await using var context = _database.CreateDbContext();
        var firstInstance = CreateRedisBackedCache("shared:");
        var secondInstance = CreateRedisBackedCache("shared:");
        var gameId = await CreateGameAsync(context, firstInstance, TestContext.Current.CancellationToken);
        var firstReader = new GetGameQueryHandler(context, new FakeTimeProvider(ReadAt), firstInstance);
        await firstReader.HandleAsync(new GetGameQuery(gameId.Value), TestContext.Current.CancellationToken);

        await context
            .Games.Where(game => game.Id == gameId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(game => game.Title, GameTitle.From("Changed behind the cache")),
                TestContext.Current.CancellationToken
            );

        // Act — a second process (separate L1) must still get the entry from Redis
        var secondReader = new GetGameQueryHandler(context, new FakeTimeProvider(ReadAt), secondInstance);
        var response = await secondReader.HandleAsync(
            new GetGameQuery(gameId.Value),
            TestContext.Current.CancellationToken
        );

        // Assert
        response.Title.ShouldBe("Portal");
    }

    [Fact]
    public async Task HandleAsync_AfterUpdateGameCommand_ShouldEvictTheCacheAndReturnTheNewTitle()
    {
        // Arrange — unique key space so this test never sees the other test's entries
        await using var context = _database.CreateDbContext();
        var cache = CreateRedisBackedCache($"tests:{Guid.NewGuid():N}:");
        var gameId = await CreateGameAsync(context, cache, TestContext.Current.CancellationToken);
        var reader = new GetGameQueryHandler(context, new FakeTimeProvider(ReadAt), cache);
        var before = await reader.HandleAsync(new GetGameQuery(gameId.Value), TestContext.Current.CancellationToken);
        before.Title.ShouldBe("Portal");

        var updater = new UpdateGameCommandHandler(context, new FakeTimeProvider(ReadAt), cache);
        await updater.HandleAsync(
            new UpdateGameCommand(
                gameId.Value,
                "Portal 2",
                "A cooperative puzzle platformer.",
                29.99m,
                Currency.Eur,
                GameStatus.Published
            ),
            TestContext.Current.CancellationToken
        );
        context.ChangeTracker.Clear();

        // Act
        var after = await reader.HandleAsync(new GetGameQuery(gameId.Value), TestContext.Current.CancellationToken);

        // Assert
        after.Title.ShouldBe("Portal 2");
    }

    [Fact]
    public async Task HandleAsync_WhenCreateGameCommandSucceeds_ShouldEvictTheGamesList()
    {
        // Arrange — cache the (empty) list before the game is created
        await using var context = _database.CreateDbContext();
        var cache = CreateRedisBackedCache($"tests:{Guid.NewGuid():N}:");
        var reader = new GetGamesQueryHandler(context, new FakeTimeProvider(ReadAt), cache);
        var before = await reader.HandleAsync(new GetGamesQuery(), TestContext.Current.CancellationToken);
        before.ShouldBeEmpty();

        // Act
        var gameId = await CreateGameAsync(context, cache, TestContext.Current.CancellationToken);
        context.ChangeTracker.Clear();
        var after = await reader.HandleAsync(new GetGamesQuery(), TestContext.Current.CancellationToken);

        // Assert
        after.ShouldContain(game => game.Id == gameId.Value);
    }

    [Fact]
    public async Task HandleAsync_WhenDeleteGameCommandSucceeds_ShouldEvictTheGame()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var cache = CreateRedisBackedCache($"tests:{Guid.NewGuid():N}:");
        var gameId = await CreateGameAsync(context, cache, TestContext.Current.CancellationToken);
        var reader = new GetGameQueryHandler(context, new FakeTimeProvider(ReadAt), cache);
        var before = await reader.HandleAsync(new GetGameQuery(gameId.Value), TestContext.Current.CancellationToken);
        before.Title.ShouldBe("Portal");

        var deleter = new DeleteGameCommandHandler(context, cache);
        await deleter.HandleAsync(new DeleteGameCommand(gameId.Value), TestContext.Current.CancellationToken);
        context.ChangeTracker.Clear();

        // Act & Assert
        await Should.ThrowAsync<NotFoundException>(() =>
            reader.HandleAsync(new GetGameQuery(gameId.Value), TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task HandleAsync_WhenCreatePromotionCommandSucceeds_ShouldEvictTheGame()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var cache = CreateRedisBackedCache($"tests:{Guid.NewGuid():N}:");
        var gameId = await CreateGameAsync(context, cache, TestContext.Current.CancellationToken);
        var reader = new GetGameQueryHandler(context, new FakeTimeProvider(ReadAt), cache);
        var before = await reader.HandleAsync(new GetGameQuery(gameId.Value), TestContext.Current.CancellationToken);
        before.ActivePromotion.ShouldBeNull();

        var promoter = new CreatePromotionCommandHandler(context, cache);
        await promoter.HandleAsync(
            new CreatePromotionCommand(gameId.Value, 25m, ReadAt.AddHours(-1), ReadAt.AddHours(1)),
            TestContext.Current.CancellationToken
        );
        context.ChangeTracker.Clear();

        // Act
        var after = await reader.HandleAsync(new GetGameQuery(gameId.Value), TestContext.Current.CancellationToken);

        // Assert
        after.ActivePromotion.ShouldNotBeNull();
        after.ActivePromotion.Discount.ShouldBe(25m);
    }

    public async ValueTask InitializeAsync() => await _database.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private HybridCache CreateRedisBackedCache(string instanceName)
    {
        var services = new ServiceCollection();
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisFixture.Container.GetConnectionString();
            options.InstanceName = instanceName;
        });
        services.AddHybridCache();

        return services.BuildServiceProvider().GetRequiredService<HybridCache>();
    }

    private static async Task<GameId> CreateGameAsync(
        CatalogDbContext context,
        HybridCache cache,
        CancellationToken cancellationToken
    )
    {
        var handler = new CreateGameCommandHandler(context, cache);
        var response = await handler.HandleAsync(
            new CreateGameCommand("Portal", "A puzzle platformer.", 19.99m, Currency.Usd),
            cancellationToken
        );

        return GameId.From(response.Id);
    }
}
