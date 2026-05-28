using Kongroo.BuildingBlocks.Domain.Exceptions;
using Kongroo.Catalog.Domain;
using Kongroo.Catalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Kongroo.Catalog.Application;

public sealed class UpdateGameCommandHandler(CatalogDbContext context, TimeProvider timeProvider)
{
    public async Task<GetGameResponse> HandleAsync(UpdateGameCommand command, CancellationToken cancellationToken)
    {
        var game =
            await context
                .Games.Include(candidate => candidate.Promotions)
                .Where(game => game.Id == GameId.From(command.GameId))
                .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Game), $"identifier '{command.GameId}'");

        game.ChangeDetails(GameTitle.From(command.Title), GameDescription.From(command.Description));
        game.ChangePrice(Money.From(command.PriceAmount, command.Currency));
        game.ChangeStatus(command.Status);

        await context.SaveChangesAsync(cancellationToken);

        var activePromotion = game.GetActivePromotion(timeProvider.GetUtcNow());

        return new GetGameResponse(
            game.Id.Value,
            game.Title.Value,
            game.Description.Value,
            game.Price.Amount,
            game.Price.Currency,
            game.Status,
            activePromotion is null
                ? null
                : new GetPromotionResponse(
                    activePromotion.Id.Value,
                    game.Id.Value,
                    activePromotion.Discount.Value,
                    activePromotion.ActiveRange.Start,
                    activePromotion.ActiveRange.End
                )
        );
    }
}

