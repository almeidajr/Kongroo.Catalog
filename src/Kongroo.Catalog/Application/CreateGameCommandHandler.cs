using Kongroo.Catalog.Domain;
using Kongroo.Catalog.Infrastructure;
using Microsoft.Extensions.Caching.Hybrid;

namespace Kongroo.Catalog.Application;

public sealed class CreateGameCommandHandler(CatalogDbContext context, HybridCache cache)
{
    public async Task<CreateGameResponse> HandleAsync(CreateGameCommand command, CancellationToken cancellationToken)
    {
        var game = Game.Create(
            GameTitle.From(command.Title),
            GameDescription.From(command.Description),
            Money.From(command.PriceAmount, command.Currency)
        );

        context.Games.Add(game);
        await context.SaveChangesAsync(cancellationToken);
        await cache.RemoveByTagAsync(GamesCache.Tag, CancellationToken.None);

        return new CreateGameResponse(
            game.Id.Value,
            game.Title.Value,
            game.Description.Value,
            game.Price.Amount,
            game.Price.Currency,
            game.Status
        );
    }
}
