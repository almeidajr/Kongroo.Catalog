using Kongroo.Catalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Kongroo.Catalog.Application;

public sealed class GetGamesQueryHandler(CatalogDbContext context, TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<GetGameResponse>> HandleAsync(
        GetGamesQuery query,
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
