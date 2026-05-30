using Kongroo.BuildingBlocks.Domain.Exceptions;
using Kongroo.Catalog.Domain;
using Kongroo.Catalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Kongroo.Catalog.Application;

public sealed class CreatePromotionCommandHandler(CatalogDbContext context)
{
    public async Task<GetPromotionResponse> HandleAsync(
        CreatePromotionCommand command,
        CancellationToken cancellationToken
    )
    {
        var game =
            await context
                .Games.Include(candidate => candidate.Promotions)
                .Where(candidate => candidate.Id == GameId.From(command.GameId))
                .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Game), $"identifier '{command.GameId}'");

        var promotion = game.CreatePromotion(
            Percentage.From(command.Discount),
            DateTimeRange.From(command.StartsAt, command.EndsAt)
        );

        await context.SaveChangesAsync(cancellationToken);

        return new GetPromotionResponse(
            promotion.Id.Value,
            game.Id.Value,
            promotion.Discount.Value,
            promotion.ActiveRange.Start,
            promotion.ActiveRange.End
        );
    }
}
