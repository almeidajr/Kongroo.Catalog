using Kongroo.BuildingBlocks.Domain.Exceptions;
using Kongroo.Catalog.Application;
using Kongroo.Catalog.Domain;
using Kongroo.Catalog.Infrastructure;
using Kongroo.Catalog.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace Kongroo.Catalog.IntegrationTests.Catalog.Application;

public sealed class ApplyPaymentResultCommandHandlerTests(PostgreSqlFixture postgreSqlFixture)
    : IClassFixture<PostgreSqlFixture>,
        IAsyncLifetime
{
    private static readonly DateTimeOffset PlacedAt = new(2026, 4, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ProcessedAt = new(2026, 4, 2, 12, 0, 0, TimeSpan.Zero);
    private const string Email = "ada@example.com";
    private const string CustomerName = "Ada Lovelace";
    private readonly CatalogTestDatabase _database = new(postgreSqlFixture);

    [Fact]
    public async Task HandleAsync_WhenApproved_ShouldMarkOrderPaidAndGrantOwnership()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var customerId = CustomerId.Create();
        var gameId = await CreatePublishedGameAsync(context, "Portal", 20m, TestContext.Current.CancellationToken);
        var orderId = await PlacePendingOrderAsync(
            context,
            customerId,
            [gameId],
            TestContext.Current.CancellationToken
        );
        var handler = new ApplyPaymentResultCommandHandler(context);

        // Act
        await handler.HandleAsync(
            new ApplyPaymentResultCommand(orderId.Value, IsApproved: true, ProcessedAt),
            TestContext.Current.CancellationToken
        );

        // Assert
        context.ChangeTracker.Clear();
        var order = await context.Orders.SingleAsync(
            candidate => candidate.Id == orderId,
            TestContext.Current.CancellationToken
        );
        order.Status.ShouldBe(OrderStatus.Paid);

        var ownerships = await context
            .Ownerships.AsNoTracking()
            .Where(ownership => ownership.OrderId == orderId)
            .ToListAsync(TestContext.Current.CancellationToken);
        ownerships.Count.ShouldBe(1);
        ownerships.ShouldAllBe(ownership => ownership.CustomerId == customerId);
        ownerships.ShouldAllBe(ownership => ownership.AcquiredAt == ProcessedAt);
    }

    [Fact]
    public async Task HandleAsync_WhenRejected_ShouldMarkOrderRejectedAndGrantNoOwnership()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var customerId = CustomerId.Create();
        var gameId = await CreatePublishedGameAsync(context, "Portal", 20m, TestContext.Current.CancellationToken);
        var orderId = await PlacePendingOrderAsync(
            context,
            customerId,
            [gameId],
            TestContext.Current.CancellationToken
        );
        var handler = new ApplyPaymentResultCommandHandler(context);

        // Act
        await handler.HandleAsync(
            new ApplyPaymentResultCommand(orderId.Value, IsApproved: false, ProcessedAt),
            TestContext.Current.CancellationToken
        );

        // Assert
        context.ChangeTracker.Clear();
        var order = await context.Orders.SingleAsync(
            candidate => candidate.Id == orderId,
            TestContext.Current.CancellationToken
        );
        order.Status.ShouldBe(OrderStatus.Rejected);
        (await context.Ownerships.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_WhenOrderMissing_ShouldThrowNotFoundException()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var handler = new ApplyPaymentResultCommandHandler(context);
        var missingOrderId = Guid.NewGuid();

        // Act
        var exception = await Should.ThrowAsync<NotFoundException>(() =>
            handler.HandleAsync(
                new ApplyPaymentResultCommand(missingOrderId, IsApproved: true, ProcessedAt),
                TestContext.Current.CancellationToken
            )
        );

        // Assert
        exception.ResourceName.ShouldBe(nameof(Order));
    }

    [Fact]
    public async Task HandleAsync_WhenOrderAlreadyDecided_ShouldBeNoOp()
    {
        // Arrange
        await using var context = _database.CreateDbContext();
        var customerId = CustomerId.Create();
        var gameId = await CreatePublishedGameAsync(context, "Portal", 20m, TestContext.Current.CancellationToken);
        var orderId = await PlacePendingOrderAsync(
            context,
            customerId,
            [gameId],
            TestContext.Current.CancellationToken
        );
        var handler = new ApplyPaymentResultCommandHandler(context);
        await handler.HandleAsync(
            new ApplyPaymentResultCommand(orderId.Value, IsApproved: true, ProcessedAt),
            TestContext.Current.CancellationToken
        );

        // Act
        await handler.HandleAsync(
            new ApplyPaymentResultCommand(orderId.Value, IsApproved: true, ProcessedAt.AddMinutes(1)),
            TestContext.Current.CancellationToken
        );

        // Assert
        context.ChangeTracker.Clear();
        var order = await context.Orders.SingleAsync(
            candidate => candidate.Id == orderId,
            TestContext.Current.CancellationToken
        );
        order.Status.ShouldBe(OrderStatus.Paid);

        var ownerships = await context
            .Ownerships.AsNoTracking()
            .Where(ownership => ownership.OrderId == orderId)
            .ToListAsync(TestContext.Current.CancellationToken);
        ownerships.Count.ShouldBe(1);
    }

    public async ValueTask InitializeAsync() => await _database.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static async Task<GameId> CreatePublishedGameAsync(
        CatalogDbContext context,
        string title,
        decimal priceAmount,
        CancellationToken cancellationToken
    )
    {
        var createHandler = new CreateGameCommandHandler(context);
        var created = await createHandler.HandleAsync(
            new CreateGameCommand(title, $"{title} description.", priceAmount, Currency.Usd),
            cancellationToken
        );

        var updateHandler = new UpdateGameCommandHandler(context, new FakeTimeProvider(PlacedAt));
        await updateHandler.HandleAsync(
            new UpdateGameCommand(
                created.Id,
                title,
                $"{title} description.",
                priceAmount,
                Currency.Usd,
                GameStatus.Published
            ),
            cancellationToken
        );

        return GameId.From(created.Id);
    }

    private static async Task<OrderId> PlacePendingOrderAsync(
        CatalogDbContext context,
        CustomerId customerId,
        IReadOnlyList<GameId> gameIds,
        CancellationToken cancellationToken
    )
    {
        var handler = new PlaceOrderCommandHandler(
            context,
            new FakeTimeProvider(PlacedAt),
            new TestUnitOfWork(context)
        );
        var response = await handler.HandleAsync(
            new PlaceOrderCommand(customerId.Value, Email, CustomerName, [.. gameIds.Select(gameId => gameId.Value)]),
            cancellationToken
        );

        return OrderId.From(response.Id);
    }
}
