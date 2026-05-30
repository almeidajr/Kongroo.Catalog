using Kongroo.BuildingBlocks.Domain.Exceptions;
using Kongroo.Catalog.Domain;
using Kongroo.Catalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Kongroo.Catalog.Application;

public sealed class GetOrderQueryHandler(CatalogDbContext context)
{
    public async Task<GetOrderResponse> HandleAsync(GetOrderQuery query, CancellationToken cancellationToken)
    {
        var buyerId = BuyerId.From(query.BuyerId);
        var order =
            await context
                .Orders.AsNoTracking()
                .Where(candidate => candidate.BuyerId == buyerId && candidate.Id == OrderId.From(query.OrderId))
                .Select(order => new GetOrderResponse(
                    order.Id.Value,
                    order.BuyerId.Value,
                    order.PurchasedAt,
                    order.Total.Amount,
                    order.Total.Currency,
                    order
                        .Lines.Select(line => new GetOrderLineResponse(
                            line.GameId.Value,
                            line.GameTitle.Value,
                            line.ListPrice.Amount,
                            line.ListPrice.Currency,
                            line.FinalPrice.Amount,
                            line.FinalPrice.Currency,
                            line.AppliedPromotionId == null ? null : line.AppliedPromotionId.Value
                        ))
                        .ToList()
                ))
                .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Order), $"identifier '{query.OrderId}'");

        return order;
    }
}
