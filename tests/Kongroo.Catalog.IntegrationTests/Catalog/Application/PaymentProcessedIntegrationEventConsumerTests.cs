using Kongroo.BuildingBlocks.Contracts;
using Kongroo.Catalog.Application;
using Kongroo.Catalog.Domain;
using Kongroo.Catalog.Infrastructure;
using Kongroo.Catalog.IntegrationTests.Fixtures;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;

namespace Kongroo.Catalog.IntegrationTests.Catalog.Application;

public sealed class PaymentProcessedIntegrationEventConsumerTests(PostgreSqlFixture postgreSqlFixture)
    : IClassFixture<PostgreSqlFixture>,
        IAsyncLifetime
{
    private const string Email = "ada@example.com";
    private const string CustomerName = "Ada Lovelace";
    private static readonly DateTimeOffset PlacedAt = new(2026, 4, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ProcessedAt = new(2026, 4, 2, 12, 0, 0, TimeSpan.Zero);
    private readonly CatalogTestDatabase _database = new(postgreSqlFixture);

    [Fact]
    public async Task Consume_WhenApproved_ShouldMarkOrderPaidAndGrantOwnership()
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
        var consumer = new PaymentProcessedIntegrationEventConsumer(new ApplyPaymentResultCommandHandler(context));

        var message = new PaymentProcessedIntegrationEvent(
            orderId.Value,
            customerId.Value,
            Email,
            CustomerName,
            20m,
            "USD",
            Approved: true,
            ProcessedAt
        );
        var consumeContext = Substitute.For<ConsumeContext<PaymentProcessedIntegrationEvent>>();
        consumeContext.Message.Returns(message);
        consumeContext.CancellationToken.Returns(TestContext.Current.CancellationToken);

        // Act
        await consumer.Consume(consumeContext);

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
        ownerships.ShouldAllBe(ownership => ownership.AcquiredAt == ProcessedAt);
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
        var handler = new PlaceOrderCommandHandler(context, new FakeTimeProvider(PlacedAt));
        var response = await handler.HandleAsync(
            new PlaceOrderCommand(customerId.Value, Email, CustomerName, [.. gameIds.Select(gameId => gameId.Value)]),
            cancellationToken
        );

        return OrderId.From(response.Id);
    }
}
