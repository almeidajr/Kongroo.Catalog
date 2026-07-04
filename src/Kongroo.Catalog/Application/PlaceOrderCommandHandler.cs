using Kongroo.BuildingBlocks.Application;
using Kongroo.BuildingBlocks.Domain.Exceptions;
using Kongroo.Catalog.Domain;
using Kongroo.Catalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Kongroo.Catalog.Application;

public sealed class PlaceOrderCommandHandler(
    CatalogDbContext context,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork
)
{
    public async Task<GetOrderResponse> HandleAsync(PlaceOrderCommand command, CancellationToken cancellationToken)
    {
        ThrowIfContainsDuplicateGames(command.GameIds);

        var requestedGameIds = command.GameIds.Select(GameId.From).ToList();

        var games = await context
            .Games.AsNoTracking()
            .Include(game => game.Promotions)
            .Where(game => requestedGameIds.Contains(game.Id))
            .ToDictionaryAsync(game => game.Id, cancellationToken);

        var missingGameId = requestedGameIds.FirstOrDefault(gameId => !games.ContainsKey(gameId));
        if (missingGameId is not null)
        {
            throw new NotFoundException(nameof(Game), $"identifier '{missingGameId.Value}'");
        }

        var customerId = CustomerId.From(command.CustomerId);
        var ownedGameIds = await context
            .Orders.AsNoTracking()
            .Where(order => order.CustomerId == customerId && order.Status != OrderStatus.Rejected)
            .SelectMany(order => order.Lines)
            .Select(line => line.GameId)
            .ToListAsync(cancellationToken);

        var alreadyOwnedGameId = requestedGameIds.FirstOrDefault(ownedGameIds.Contains);
        if (alreadyOwnedGameId is not null)
        {
            throw new ConflictException(nameof(Order), $"customer already ordered game '{alreadyOwnedGameId.Value}'");
        }

        var purchasedAt = timeProvider.GetUtcNow();

        var quotes = games.Values.Select(game => game.QuotePurchase(purchasedAt)).ToList();
        var order = Order.Place(customerId, command.Email, command.CustomerName, quotes, purchasedAt);

        context.Orders.Add(order);
        await unitOfWork.CommitAsync(cancellationToken);

        return new GetOrderResponse(
            order.Id.Value,
            order.CustomerId.Value,
            order.Status,
            order.PurchasedAt,
            order.Total.Amount,
            order.Total.Currency,
            [
                .. order
                    .Lines.OrderBy(line => line.GameTitle.Value)
                    .Select(line => new GetOrderLineResponse(
                        line.GameId.Value,
                        line.GameTitle.Value,
                        line.ListPrice.Amount,
                        line.ListPrice.Currency,
                        line.FinalPrice.Amount,
                        line.FinalPrice.Currency,
                        line.AppliedPromotionId?.Value
                    )),
            ]
        );
    }

    private static void ThrowIfContainsDuplicateGames(IReadOnlyList<Guid> gameIds)
    {
        var hasDuplicates = gameIds.GroupBy(gameId => gameId).Any(group => group.Count() > 1);

        if (hasDuplicates)
        {
            throw new ConflictException(nameof(Order), "cannot contain duplicate games");
        }
    }
}
