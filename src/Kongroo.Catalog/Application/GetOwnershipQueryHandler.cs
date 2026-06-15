using Kongroo.BuildingBlocks.Domain.Exceptions;
using Kongroo.Catalog.Domain;
using Kongroo.Catalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Kongroo.Catalog.Application;

public sealed class GetOwnershipQueryHandler(CatalogDbContext context)
{
    public async Task<GetOwnershipResponse> HandleAsync(GetOwnershipQuery query, CancellationToken cancellationToken) =>
        await context
            .Ownerships.AsNoTracking()
            .Where(ownership =>
                ownership.Id == OwnershipId.From(query.OwnershipId)
                && ownership.CustomerId == CustomerId.From(query.CustomerId)
            )
            .Select(ownership => new GetOwnershipResponse(
                ownership.Id.Value,
                ownership.CustomerId.Value,
                ownership.GameId.Value,
                ownership.OrderId.Value,
                ownership.AcquiredAt
            ))
            .SingleOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException(nameof(Ownership), $"identifier '{query.OwnershipId}'");
}
