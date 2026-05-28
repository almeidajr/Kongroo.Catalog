using Kongroo.BuildingBlocks.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Kongroo.BuildingBlocks.Infrastructure;

public sealed class OutboxMessagesInterceptor : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default
    )
    {
        PersistOutboxMessages(eventData.Context);

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void PersistOutboxMessages(DbContext? context)
    {
        if (context is not OutboxDbContext outboxContext)
        {
            return;
        }

        var entities = context
            .ChangeTracker.Entries<IHasDomainEvents>()
            .Select(entry => entry.Entity)
            .Where(entity => entity.DomainEvents.Count > 0)
            .ToList();

        if (entities.Count == 0)
        {
            return;
        }

        outboxContext.OutboxMessages.AddRange(
            entities.SelectMany(entity => entity.DomainEvents).Select(OutboxMessage.Create)
        );

        foreach (var entity in entities)
        {
            entity.ClearDomainEvents();
        }
    }
}
