using Kongroo.Catalog.Domain;
using Kongroo.Catalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Kongroo.Catalog.Application;

public sealed class GetOwnershipsQueryHandler(CatalogDbContext context)
{
    public async Task<IReadOnlyList<GetOwnershipResponse>> HandleAsync(
        GetOwnershipsQuery query,
        CancellationToken cancellationToken
    ) =>
        await context
            .Ownerships.AsNoTracking()
            .Where(ownership => ownership.CustomerId == CustomerId.From(query.CustomerId))
            .OrderByDescending(ownership => ownership.AcquiredAt)
            .ThenByDescending(ownership => ownership.Id)
            .Select(ownership => new GetOwnershipResponse(
                ownership.Id.Value,
                ownership.CustomerId.Value,
                ownership.GameId.Value,
                ownership.OrderId.Value,
                ownership.AcquiredAt
            ))
            .ToListAsync(cancellationToken);
}
