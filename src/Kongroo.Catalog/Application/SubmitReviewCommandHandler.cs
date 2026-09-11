using Kongroo.BuildingBlocks.Domain.Exceptions;
using Kongroo.Catalog.Domain;
using Kongroo.Catalog.Infrastructure;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

namespace Kongroo.Catalog.Application;

public sealed class SubmitReviewCommandHandler(
    CatalogDbContext context,
    IMongoCollection<ReviewDocument> reviews,
    TimeProvider timeProvider
)
{
    public const string ResourceName = "Review";

    public async Task<GetReviewResponse> HandleAsync(SubmitReviewCommand command, CancellationToken cancellationToken)
    {
        var gameExists = await context
            .Games.AsNoTracking()
            .AnyAsync(game => game.Id == GameId.From(command.GameId), cancellationToken);
        if (!gameExists)
        {
            throw new NotFoundException(nameof(Game), $"identifier '{command.GameId}'");
        }

        var document = new ReviewDocument
        {
            Id = Guid.CreateVersion7(),
            GameId = command.GameId,
            CustomerId = command.CustomerId,
            CustomerName = command.CustomerName,
            Rating = command.Rating,
            Text = command.Text,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        };

        try
        {
            await reviews.InsertOneAsync(document, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException exception)
            when (exception.WriteError is { Category: ServerErrorCategory.DuplicateKey })
        {
            throw new ConflictException(ResourceName, $"customer already reviewed game '{command.GameId}'", exception);
        }

        return document.ToResponse();
    }
}
