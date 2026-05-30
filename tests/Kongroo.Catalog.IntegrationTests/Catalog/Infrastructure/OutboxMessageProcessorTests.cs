using Kongroo.BuildingBlocks.Application;
using Kongroo.BuildingBlocks.Domain;
using Kongroo.BuildingBlocks.Infrastructure;
using Kongroo.Catalog.Infrastructure;
using Kongroo.Catalog.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace Kongroo.Catalog.IntegrationTests.Catalog.Infrastructure;

public sealed class OutboxMessageProcessorTests(PostgreSqlFixture postgreSqlFixture)
    : IClassFixture<PostgreSqlFixture>,
        IAsyncLifetime
{
    private static readonly DateTimeOffset ProcessedAt = new(2026, 4, 2, 12, 0, 0, TimeSpan.Zero);
    private readonly CatalogTestDatabase _database = new(postgreSqlFixture);

    [Fact]
    public async Task ProcessPendingMessagesAsync_WithMatchingHandler_ShouldMarkMessageProcessed()
    {
        // Arrange
        var domainEvent = new TestDomainEvent();
        var outboxMessage = await SaveOutboxMessageAsync(domainEvent);
        var handledEventIds = new List<Guid>();

        await using var context = _database.CreateDbContext();
        var processor = CreateProcessor(
            context,
            new FakeTimeProvider(ProcessedAt),
            new RecordingDomainEventHandler<TestDomainEvent>(handledEventIds)
        );

        // Act
        await processor.ProcessPendingMessagesAsync(TestContext.Current.CancellationToken);

        // Assert
        handledEventIds.ShouldBe([domainEvent.Id]);

        var persistedMessage = await context.OutboxMessages.SingleAsync(
            message => message.Id == outboxMessage.Id,
            TestContext.Current.CancellationToken
        );

        persistedMessage.ProcessedAt.ShouldBe(ProcessedAt);
    }

    [Fact]
    public async Task ProcessPendingMessagesAsync_WithNoMatchingHandler_ShouldMarkMessageProcessed()
    {
        // Arrange
        var domainEvent = new UnhandledTestDomainEvent();
        var outboxMessage = await SaveOutboxMessageAsync(domainEvent);

        await using var context = _database.CreateDbContext();
        var processor = CreateProcessor(context, new FakeTimeProvider(ProcessedAt));

        // Act
        await processor.ProcessPendingMessagesAsync(TestContext.Current.CancellationToken);

        // Assert
        var persistedMessage = await context.OutboxMessages.SingleAsync(
            message => message.Id == outboxMessage.Id,
            TestContext.Current.CancellationToken
        );

        persistedMessage.ProcessedAt.ShouldBe(ProcessedAt);
    }

    [Fact]
    public async Task ProcessPendingMessagesAsync_WhenHandlerThrows_ShouldRetryMessageOnLaterPass()
    {
        // Arrange
        var domainEvent = new RetryTestDomainEvent();
        var outboxMessage = await SaveOutboxMessageAsync(domainEvent);

        await using var context = _database.CreateDbContext();

        var failingProcessor = CreateProcessor(
            context,
            new FakeTimeProvider(ProcessedAt),
            new ThrowingDomainEventHandler<RetryTestDomainEvent>("Simulated handler failure.")
        );

        // Act
        await failingProcessor.ProcessPendingMessagesAsync(TestContext.Current.CancellationToken);

        // Assert
        var failedMessage = await context.OutboxMessages.SingleAsync(
            message => message.Id == outboxMessage.Id,
            TestContext.Current.CancellationToken
        );

        failedMessage.ProcessedAt.ShouldBeNull();
        failedMessage.FailedAt.ShouldBe(ProcessedAt);
        failedMessage.Error.ShouldBe("Simulated handler failure.");

        var handledEventIds = new List<Guid>();

        var retryProcessor = CreateProcessor(
            context,
            new FakeTimeProvider(ProcessedAt.AddMinutes(1)),
            new RecordingDomainEventHandler<RetryTestDomainEvent>(handledEventIds)
        );

        // Act
        await retryProcessor.ProcessPendingMessagesAsync(TestContext.Current.CancellationToken);

        // Assert
        handledEventIds.ShouldBe([domainEvent.Id]);

        var retriedMessage = await context.OutboxMessages.SingleAsync(
            message => message.Id == outboxMessage.Id,
            TestContext.Current.CancellationToken
        );

        retriedMessage.ProcessedAt.ShouldBe(ProcessedAt.AddMinutes(1));
        retriedMessage.FailedAt.ShouldBeNull();
        retriedMessage.Error.ShouldBeNull();
    }

    public async ValueTask InitializeAsync() => await _database.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private async Task<OutboxMessage> SaveOutboxMessageAsync(DomainEvent domainEvent)
    {
        var outboxMessage = OutboxMessage.Create(domainEvent);

        await using var context = _database.CreateDbContext();
        context.OutboxMessages.Add(outboxMessage);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        return outboxMessage;
    }

    private static OutboxMessageProcessor<CatalogDbContext> CreateProcessor(
        CatalogDbContext context,
        FakeTimeProvider timeProvider,
        params IDomainEventHandler[] handlers
    ) =>
        new(
            NullLogger<OutboxMessageProcessor<CatalogDbContext>>.Instance,
            context,
            handlers,
            timeProvider,
            Options.Create(new OutboxProcessingOptions { PollingInterval = TimeSpan.FromSeconds(1), BatchSize = 20 })
        );

    private sealed record TestDomainEvent : DomainEvent;

    private sealed record UnhandledTestDomainEvent : DomainEvent;

    private sealed record RetryTestDomainEvent : DomainEvent;

    private sealed class RecordingDomainEventHandler<TDomainEvent>(List<Guid> handledEventIds)
        : DomainEventHandler<TDomainEvent>
        where TDomainEvent : DomainEvent
    {
        public override Task HandleAsync(TDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            handledEventIds.Add(domainEvent.Id);

            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingDomainEventHandler<TDomainEvent>(string message) : DomainEventHandler<TDomainEvent>
        where TDomainEvent : DomainEvent
    {
        public override Task HandleAsync(TDomainEvent domainEvent, CancellationToken cancellationToken) =>
            throw new InvalidOperationException(message);
    }
}
