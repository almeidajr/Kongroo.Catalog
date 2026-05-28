using Kongroo.BuildingBlocks.Application;
using Kongroo.BuildingBlocks.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kongroo.BuildingBlocks.Infrastructure;

public sealed class OutboxMessageProcessor<TDbContext>(
    ILogger<OutboxMessageProcessor<TDbContext>> logger,
    TDbContext context,
    IEnumerable<IDomainEventHandler> handlers,
    TimeProvider timeProvider,
    IOptions<OutboxProcessingOptions> options
)
    where TDbContext : OutboxDbContext<TDbContext>, IRelationalDbContext
{
    private readonly OutboxProcessingOptions _options = options.Value;

    public async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        var messages = await LoadPendingMessagesAsync(cancellationToken);

        foreach (var message in messages)
        {
            await ProcessMessageAsync(message, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<OutboxMessage>> LoadPendingMessagesAsync(CancellationToken cancellationToken) =>
        await context
            .Set<OutboxMessage>()
            .Where(message => message.ProcessedAt == null)
            .OrderByDescending(message => message.FailedAt != null)
            .ThenBy(message => message.FailedAt)
            .ThenBy(message => message.OccurredAt)
            .ThenBy(message => message.Id)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken);

    private async Task ProcessMessageAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        try
        {
            var domainEvent = message.GetDomainEvent();
            await DispatchAsync(domainEvent, cancellationToken);

            message.MarkProcessed(timeProvider.GetUtcNow());
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Failed to process outbox message {OutboxMessageId} for event type {EventType}.",
                message.Id.Value,
                message.EventType
            );

            message.MarkFailed(timeProvider.GetUtcNow(), exception.Message);
        }
    }

    private async Task DispatchAsync(DomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var matchingHandlers = handlers.Where(handler => handler.EventType == domainEvent.GetType());

        foreach (var handler in matchingHandlers)
        {
            await handler.HandleAsync(domainEvent, cancellationToken);
        }
    }
}
