using Kongroo.Catalog.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace Kongroo.Catalog.Application;

public sealed class GetGamesQueryHandler(CatalogDbContext context, TimeProvider timeProvider, HybridCache cache)
{
    public async Task<IReadOnlyList<GetGameResponse>> HandleAsync(
        GetGamesQuery query,
        CancellationToken cancellationToken
    ) =>
        // ponytail: the request-scoped DbContext is shared with stampede-joined callers; if the
        // first caller aborts mid-query they get one failed request. Switch to IServiceScopeFactory
        // if it ever shows up.
        await cache.GetOrCreateAsync(
            GamesCache.ListKey,
            (context, timeProvider),
            static (state, token) => LoadAsync(state.context, state.timeProvider, token),
            tags: [GamesCache.Tag],
            cancellationToken: cancellationToken
        );

    private static async ValueTask<IReadOnlyList<GetGameResponse>> LoadAsync(
        CatalogDbContext context,
        TimeProvider timeProvider,
        CancellationToken cancellationToken
    )
    {
        var now = timeProvider.GetUtcNow();

        return await context
            .Games.AsNoTracking()
            .OrderBy(game => game.Title)
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
            .ToListAsync(cancellationToken);
    }
}
