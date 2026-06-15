using Kongroo.Catalog.Domain;
using Kongroo.Catalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Kongroo.Catalog.Application;

public sealed class GetGameOwnershipsQueryHandler(CatalogDbContext context)
{
    public async Task<IReadOnlyList<GetGameOwnershipResponse>> HandleAsync(
        GetGameOwnershipsQuery query,
        CancellationToken cancellationToken
    ) =>
        await context
            .GameOwnerships.AsNoTracking()
            .Where(ownership => ownership.OwnerId == OwnerId.From(query.OwnerId))
            .OrderByDescending(ownership => ownership.AcquiredAt)
            .ThenByDescending(ownership => ownership.Id)
            .Select(ownership => new GetGameOwnershipResponse(
                ownership.Id.Value,
                ownership.OwnerId.Value,
                ownership.GameId.Value,
                ownership.OrderId.Value,
                ownership.AcquiredAt
            ))
            .ToListAsync(cancellationToken);
}
