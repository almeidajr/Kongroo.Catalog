using Kongroo.BuildingBlocks.Domain.Exceptions;
using Kongroo.Catalog.Domain;
using Kongroo.Catalog.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace Kongroo.Catalog.Application;

public sealed class GetGameQueryHandler(CatalogDbContext context, TimeProvider timeProvider, HybridCache cache)
{
    public async Task<GetGameResponse> HandleAsync(GetGameQuery query, CancellationToken cancellationToken) =>
        // ponytail: the request-scoped DbContext is shared with stampede-joined callers; if the
        // first caller aborts mid-query they get one failed request. Switch to IServiceScopeFactory
        // if it ever shows up.
        await cache.GetOrCreateAsync(
            GamesCache.GameKey(query.GameId),
            (context, timeProvider, query),
            static (state, token) => LoadAsync(state.context, state.timeProvider, state.query, token),
            tags: [GamesCache.Tag],
            cancellationToken: cancellationToken
        );

    private static async ValueTask<GetGameResponse> LoadAsync(
        CatalogDbContext context,
        TimeProvider timeProvider,
        GetGameQuery query,
        CancellationToken cancellationToken
    )
    {
        var now = timeProvider.GetUtcNow();

        return await context
                .Games.AsNoTracking()
                .Where(game => game.Id == GameId.From(query.GameId))
                .Select(game => new GetGameResponse(
                    game.Id.Value,
                    game.Title.Value,
                    game.Description.Value,
                    game.Price.Amount,
                    game.Price.Currency,
                    game.Status,
                    game.Promotions.Where(promotion =>
                            promotion.ActiveRange.Start <= now && now < promotion.ActiveRange.End
                        )
                        .Select(promotion => new GetPromotionResponse(
                            promotion.Id.Value,
                            game.Id.Value,
                            promotion.Discount.Value,
                            promotion.ActiveRange.Start,
                            promotion.ActiveRange.End
                        ))
                        .SingleOrDefault()
                ))
                .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Game), $"identifier '{query.GameId}'");
    }
}
