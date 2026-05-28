using Kongroo.Catalog.Domain;
using Kongroo.Catalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Kongroo.Catalog.Application;

public sealed class GetOrdersQueryHandler(CatalogDbContext context)
{
    public async Task<IReadOnlyList<GetOrderResponse>> HandleAsync(
        GetOrdersQuery query,
        CancellationToken cancellationToken
    )
    {
        var buyerId = BuyerId.From(query.BuyerId);
        return await context
            .Orders.AsNoTracking()
            .Where(order => order.BuyerId == buyerId)
            .OrderByDescending(order => order.PurchasedAt)
            .ThenByDescending(order => order.Id)
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
            .ToListAsync(cancellationToken);
    }
}

