using Kongroo.BuildingBlocks.Application;
using Kongroo.BuildingBlocks.Contracts;
using Kongroo.BuildingBlocks.Infrastructure;
using Kongroo.Catalog.Application;
using Kongroo.Catalog.Domain;
using Kongroo.Catalog.Infrastructure;
using Kongroo.Catalog.IntegrationTests.Fixtures;
using Kongroo.Catalog.IntegrationTests.Library;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace Kongroo.Catalog.IntegrationTests.Catalog.Infrastructure;

public sealed class CatalogToLibraryIntegrationTests(PostgreSqlFixture postgreSqlFixture)
    : IClassFixture<PostgreSqlFixture>,
        IAsyncLifetime
{
    private static readonly DateTimeOffset PurchasedAt = new(2026, 4, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ProcessedAt = new(2026, 4, 2, 12, 0, 0, TimeSpan.Zero);
    private readonly CatalogTestDatabase _catalogDatabase = new(postgreSqlFixture);
    private readonly LibraryTestDatabase _libraryDatabase = new(postgreSqlFixture);

    [Fact]
    public async Task ProcessPendingMessagesAsync_WithPlacedOrder_ShouldCreateLibraryOwnerships()
    {
        // Arrange
        var orderId = await PlaceCompletedOrderAsync(["Portal", "Half-Life"]);

        await using var serviceProvider = CreateServiceProvider(services =>
        {
            services.AddLogging();
            services.AddScoped<IEventBus, InProcessEventBus>();
            services.AddScoped(_ => _libraryDatabase.CreateDbContext());
            services.AddScoped<
                IIntegrationEventHandler<OrderCompletedIntegrationEvent>,
                OrderCompletedIntegrationEventHandler
            >();
        });
        await using var scope = serviceProvider.CreateAsyncScope();
        var handler = new OrderPlacedDomainEventHandler(scope.ServiceProvider.GetRequiredService<IEventBus>());

        await using var catalogContext = _catalogDatabase.CreateDbContext();
        var processor = CreateProcessor(catalogContext, ProcessedAt, handler);

        // Act
        await processor.ProcessPendingMessagesAsync(TestContext.Current.CancellationToken);

        // Assert
        var outboxMessage = await GetOrderPlacedOutboxMessageAsync(catalogContext);
        outboxMessage.ProcessedAt.ShouldBe(ProcessedAt);

        await using var libraryContext = _libraryDatabase.CreateDbContext();
        var ownerships = await libraryContext
            .GameOwnerships.AsNoTracking()
            .ToListAsync(TestContext.Current.CancellationToken);

        ownerships.Count.ShouldBe(2);
        ownerships.All(ownership => ownership.OrderId.Value == orderId.Value).ShouldBeTrue();
    }

    [Fact]
    public async Task ProcessPendingMessagesAsync_WhenLibraryHandlerThrows_ShouldRetryMessageOnLaterPass()
    {
        // Arrange
        await PlaceCompletedOrderAsync(["Portal"]);

        await using var failingServiceProvider = CreateServiceProvider(services =>
        {
            services.AddLogging();
            services.AddScoped<IEventBus, InProcessEventBus>();
            services.AddScoped<IIntegrationEventHandler<OrderCompletedIntegrationEvent>>(
                _ => new ThrowingOrderCompletedIntegrationEventHandler("Simulated library failure.")
            );
        });
        await using var failingScope = failingServiceProvider.CreateAsyncScope();
        var failingHandler = new OrderPlacedDomainEventHandler(
            failingScope.ServiceProvider.GetRequiredService<IEventBus>()
        );

        await using var catalogContext = _catalogDatabase.CreateDbContext();
        var failingProcessor = CreateProcessor(catalogContext, ProcessedAt, failingHandler);

        // Act
        await failingProcessor.ProcessPendingMessagesAsync(TestContext.Current.CancellationToken);

        // Assert
        var failedMessage = await GetOrderPlacedOutboxMessageAsync(catalogContext);
        failedMessage.ProcessedAt.ShouldBeNull();
        failedMessage.FailedAt.ShouldBe(ProcessedAt);
        failedMessage.Error.ShouldBe("Simulated library failure.");

        await using var libraryContext = _libraryDatabase.CreateDbContext();
        (await libraryContext.GameOwnerships.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(0);

        await using var retryServiceProvider = CreateServiceProvider(services =>
        {
            services.AddLogging();
            services.AddScoped<IEventBus, InProcessEventBus>();
            services.AddScoped(_ => _libraryDatabase.CreateDbContext());
            services.AddScoped<
                IIntegrationEventHandler<OrderCompletedIntegrationEvent>,
                OrderCompletedIntegrationEventHandler
            >();
        });
        await using var retryScope = retryServiceProvider.CreateAsyncScope();
        var retryHandler = new OrderPlacedDomainEventHandler(
            retryScope.ServiceProvider.GetRequiredService<IEventBus>()
        );
        var retryProcessor = CreateProcessor(catalogContext, ProcessedAt.AddMinutes(1), retryHandler);

        // Act
        await retryProcessor.ProcessPendingMessagesAsync(TestContext.Current.CancellationToken);

        // Assert
        var retriedMessage = await GetOrderPlacedOutboxMessageAsync(catalogContext);
        retriedMessage.ProcessedAt.ShouldBe(ProcessedAt.AddMinutes(1));
        retriedMessage.FailedAt.ShouldBeNull();
        retriedMessage.Error.ShouldBeNull();

        (await libraryContext.GameOwnerships.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(1);
    }

    [Fact]
    public async Task ProcessPendingMessagesAsync_WhenOrderCompletedEventIsReplayed_ShouldFailReplayMessage()
    {
        // Arrange
        await PlaceCompletedOrderAsync(["Portal", "Half-Life"]);

        await using var serviceProvider = CreateServiceProvider(services =>
        {
            services.AddLogging();
            services.AddScoped<IEventBus, InProcessEventBus>();
            services.AddScoped(_ => _libraryDatabase.CreateDbContext());
            services.AddScoped<
                IIntegrationEventHandler<OrderCompletedIntegrationEvent>,
                OrderCompletedIntegrationEventHandler
            >();
        });
        await using var scope = serviceProvider.CreateAsyncScope();
        var handler = new OrderPlacedDomainEventHandler(scope.ServiceProvider.GetRequiredService<IEventBus>());

        await using var catalogContext = _catalogDatabase.CreateDbContext();
        var processor = CreateProcessor(catalogContext, ProcessedAt, handler);
        await processor.ProcessPendingMessagesAsync(TestContext.Current.CancellationToken);

        var originalMessage = await GetOrderPlacedOutboxMessageAsync(catalogContext);
        var replayMessage = OutboxMessage.Create(originalMessage.GetDomainEvent());
        catalogContext.OutboxMessages.Add(replayMessage);
        await catalogContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var replayProcessor = CreateProcessor(catalogContext, ProcessedAt.AddMinutes(1), handler);

        // Act
        await replayProcessor.ProcessPendingMessagesAsync(TestContext.Current.CancellationToken);

        // Assert
        await using var libraryContext = _libraryDatabase.CreateDbContext();
        var ownerships = await libraryContext
            .GameOwnerships.AsNoTracking()
            .ToListAsync(TestContext.Current.CancellationToken);

        ownerships.Count.ShouldBe(2);

        var persistedReplayMessage = await catalogContext.OutboxMessages.SingleAsync(
            message => message.Id == replayMessage.Id,
            TestContext.Current.CancellationToken
        );
        persistedReplayMessage.ProcessedAt.ShouldBeNull();
        persistedReplayMessage.FailedAt.ShouldBe(ProcessedAt.AddMinutes(1));
        persistedReplayMessage.Error.ShouldNotBeNull();
    }

    public async ValueTask InitializeAsync()
    {
        await _catalogDatabase.ResetAsync(TestContext.Current.CancellationToken);
        await _libraryDatabase.ResetAsync(TestContext.Current.CancellationToken);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static OutboxMessageProcessor<CatalogDbContext> CreateProcessor(
        CatalogDbContext context,
        DateTimeOffset processedAt,
        params IDomainEventHandler[] handlers
    ) =>
        new(
            NullLogger<OutboxMessageProcessor<CatalogDbContext>>.Instance,
            context,
            handlers,
            new FakeTimeProvider(processedAt),
            Options.Create(new OutboxProcessingOptions { PollingInterval = TimeSpan.FromSeconds(1), BatchSize = 20 })
        );

    private static ServiceProvider CreateServiceProvider(Action<IServiceCollection> configureServices)
    {
        var services = new ServiceCollection();
        configureServices(services);

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private async Task<OrderId> PlaceCompletedOrderAsync(IReadOnlyList<string> gameTitles)
    {
        await using var context = _catalogDatabase.CreateDbContext();
        var gameIds = new List<Guid>(gameTitles.Count);

        foreach (var gameTitle in gameTitles)
        {
            var gameId = await CreatePublishedGameAsync(context, gameTitle, 20m, TestContext.Current.CancellationToken);
            gameIds.Add(gameId.Value);
        }

        var handler = new PlaceOrderCommandHandler(context, new FakeTimeProvider(PurchasedAt));
        var response = await handler.HandleAsync(
            new PlaceOrderCommand(BuyerId.Create().Value, gameIds),
            TestContext.Current.CancellationToken
        );

        return OrderId.From(response.Id);
    }

    private static async Task<OutboxMessage> GetOrderPlacedOutboxMessageAsync(CatalogDbContext context)
    {
        var outboxMessages = await context
            .OutboxMessages.AsNoTracking()
            .ToListAsync(TestContext.Current.CancellationToken);
        return outboxMessages.Single(message => message.GetDomainEvent() is OrderPlacedDomainEvent);
    }

    private static async Task<GameId> CreatePublishedGameAsync(
        CatalogDbContext context,
        string title,
        decimal priceAmount,
        CancellationToken cancellationToken
    )
    {
        var createHandler = new CreateGameCommandHandler(context);
        var createResponse = await createHandler.HandleAsync(
            new CreateGameCommand(title, $"{title} description.", priceAmount, Currency.Usd),
            cancellationToken
        );

        var updateHandler = new UpdateGameCommandHandler(context, new FakeTimeProvider(PurchasedAt));
        await updateHandler.HandleAsync(
            new UpdateGameCommand(
                createResponse.Id,
                title,
                $"{title} description.",
                priceAmount,
                Currency.Usd,
                GameStatus.Published
            ),
            cancellationToken
        );

        return GameId.From(createResponse.Id);
    }

    private sealed class ThrowingOrderCompletedIntegrationEventHandler(string message)
        : IIntegrationEventHandler<OrderCompletedIntegrationEvent>
    {
        public Task HandleAsync(OrderCompletedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(integrationEvent);

            throw new InvalidOperationException(message);
        }
    }
}
