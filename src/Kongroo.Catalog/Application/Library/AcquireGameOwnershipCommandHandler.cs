using Kongroo.BuildingBlocks.Domain.Exceptions;
using Kongroo.Catalog.Domain;
using Kongroo.Catalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Kongroo.Catalog.Application;

public sealed class AcquireGameOwnershipCommandHandler(LibraryDbContext context)
{
    public async Task<GetGameOwnershipResponse> HandleAsync(
        AcquireGameOwnershipCommand command,
        CancellationToken cancellationToken
    )
    {
        var ownerId = OwnerId.From(command.OwnerId);
        var gameId = GameId.From(command.GameId);
        var orderId = OrderId.From(command.OrderId);

        var ownershipExists = await context
            .GameOwnerships.AsNoTracking()
            .AnyAsync(candidate => candidate.OwnerId == ownerId && candidate.GameId == gameId, cancellationToken);
        if (ownershipExists)
        {
            throw new ConflictException(nameof(GameOwnership), $"owner already owns game '{command.GameId}'");
        }

        var ownership = GameOwnership.AcquireFromOrder(ownerId, gameId, orderId, command.AcquiredAt);

        context.GameOwnerships.Add(ownership);
        await context.SaveChangesAsync(cancellationToken);

        return new GetGameOwnershipResponse(
            ownership.Id.Value,
            ownership.OwnerId.Value,
            ownership.GameId.Value,
            ownership.OrderId.Value,
            ownership.AcquiredAt
        );
    }
}
