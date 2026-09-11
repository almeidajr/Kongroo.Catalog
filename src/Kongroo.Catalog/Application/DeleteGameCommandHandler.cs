using Kongroo.BuildingBlocks.Domain.Exceptions;
using Kongroo.Catalog.Domain;
using Kongroo.Catalog.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace Kongroo.Catalog.Application;

public sealed class DeleteGameCommandHandler(CatalogDbContext context, HybridCache cache)
{
    public async Task HandleAsync(DeleteGameCommand command, CancellationToken cancellationToken)
    {
        var game =
            await context
                .Games.Where(game => game.Id == GameId.From(command.GameId))
                .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Game), $"identifier '{command.GameId}'");

        // ponytail: reviews of a deleted game stay in MongoDB, unreachable through the API; cascade
        // here if it ever matters.
        context.Games.Remove(game);
        await context.SaveChangesAsync(cancellationToken);
        await cache.RemoveByTagAsync(GamesCache.Tag, CancellationToken.None);
    }
}
