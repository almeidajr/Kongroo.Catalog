using Kongroo.Catalog.Application;
using Kongroo.Catalog.Domain;
using Kongroo.Catalog.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Kongroo.Catalog.IntegrationTests.Catalog.Application;

public sealed class CreateGameCommandHandlerTests(PostgreSqlFixture postgreSqlFixture)
    : IClassFixture<PostgreSqlFixture>,
        IAsyncLifetime
{
    private readonly CatalogTestDatabase _database = new(postgreSqlFixture);

    [Fact]
    public async Task HandleAsync_WithValidCommand_ShouldReturnCreatedGameResponse()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var handler = new CreateGameCommandHandler(context);

        // Act
        var response = await handler.HandleAsync(
            new CreateGameCommand("Portal", "A puzzle platformer.", 19.99m, Currency.Usd),
            TestContext.Current.CancellationToken
        );

        // Assert
        response.ShouldSatisfyAllConditions(
            () => response.Id.ShouldNotBe(Guid.Empty),
            () => response.Title.ShouldBe("Portal"),
            () => response.Description.ShouldBe("A puzzle platformer."),
            () => response.PriceAmount.ShouldBe(19.99m),
            () => response.Currency.ShouldBe(Currency.Usd),
            () => response.Status.ShouldBe(GameStatus.Draft)
        );
    }

    [Fact]
    public async Task HandleAsync_WithValidCommand_ShouldPersistGame()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var handler = new CreateGameCommandHandler(context);

        // Act
        var response = await handler.HandleAsync(
            new CreateGameCommand("Portal", "A puzzle platformer.", 19.99m, Currency.Usd),
            TestContext.Current.CancellationToken
        );

        context.ChangeTracker.Clear();
        var savedGame = await context.Games.SingleAsync(
            game => game.Id == GameId.From(response.Id),
            TestContext.Current.CancellationToken
        );

        // Assert
        savedGame.ShouldSatisfyAllConditions(
            () => savedGame.Title.Value.ShouldBe("Portal"),
            () => savedGame.Description.Value.ShouldBe("A puzzle platformer."),
            () => savedGame.Price.Amount.ShouldBe(19.99m),
            () => savedGame.Price.Currency.ShouldBe(Currency.Usd),
            () => savedGame.Status.ShouldBe(GameStatus.Draft)
        );
    }

    public async ValueTask InitializeAsync() => await _database.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

