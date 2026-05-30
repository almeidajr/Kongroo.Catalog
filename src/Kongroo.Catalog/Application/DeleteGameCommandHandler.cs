using Kongroo.BuildingBlocks.Domain.Exceptions;
using Kongroo.Catalog.Domain;
using Kongroo.Catalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Kongroo.Catalog.Application;

public sealed class DeleteGameCommandHandler(CatalogDbContext context)
{
    public async Task HandleAsync(DeleteGameCommand command, CancellationToken cancellationToken)
    {
        var game =
            await context
                .Games.Where(game => game.Id == GameId.From(command.GameId))
                .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Game), $"identifier '{command.GameId}'");

        context.Games.Remove(game);
        await context.SaveChangesAsync(cancellationToken);
    }
}
