using System.Text.Json;
using Kongroo.BuildingBlocks.Domain;

namespace Kongroo.BuildingBlocks.Infrastructure;

public sealed class OutboxMessage : Entity<OutboxMessageId>
{
    public const int EventTypeMaxLength = 512;
    public const int ErrorMaxLength = 2048;

    private OutboxMessage() { }

    public required DateTimeOffset OccurredAt { get; init; }

    public required string EventType { get; init; }

    public required string Payload { get; init; }

    public DateTimeOffset? ProcessedAt { get; private set; }

    public DateTimeOffset? FailedAt { get; private set; }

    public string? Error { get; private set; }

    public static OutboxMessage Create(DomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var domainEventType = domainEvent.GetType();
        var eventType =
            domainEventType.AssemblyQualifiedName
            ?? throw new InvalidOperationException($"Unable to persist the type '{domainEventType}'.");

        if (eventType.Length > EventTypeMaxLength)
        {
            throw new InvalidOperationException(
                $"The outbox event type '{domainEventType}' exceeds the maximum length of {EventTypeMaxLength} characters."
            );
        }

        var payload = JsonSerializer.Serialize(domainEvent, domainEventType);

        return new OutboxMessage
        {
            Id = OutboxMessageId.Create(),
            OccurredAt = domainEvent.OccurredAt,
            EventType = eventType,
            Payload = payload,
        };
    }

    public DomainEvent GetDomainEvent()
    {
        var domainEventType =
            Type.GetType(EventType, throwOnError: true)
            ?? throw new InvalidOperationException($"The type '{EventType}' could not be resolved.");

        if (!domainEventType.IsAssignableTo(typeof(DomainEvent)))
        {
            throw new InvalidOperationException($"The type '{EventType}' is not a domain event.");
        }

        var domainEvent =
            JsonSerializer.Deserialize(Payload, domainEventType)
            ?? throw new InvalidOperationException("Unable to deserialize the outbox payload.");

        return (DomainEvent)domainEvent;
    }

    public void MarkProcessed(DateTimeOffset processedAt)
    {
        ProcessedAt = processedAt;

        FailedAt = null;
        Error = null;
    }

    public void MarkFailed(DateTimeOffset failedAt, string error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(error.Length, ErrorMaxLength);

        FailedAt = failedAt;
        Error = error;

        ProcessedAt = null;
    }
}
