using Kongroo.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kongroo.Catalog.Infrastructure;

public sealed class GameOwnershipConfiguration : IEntityTypeConfiguration<GameOwnership>
{
    public void Configure(EntityTypeBuilder<GameOwnership> builder)
    {
        builder.HasKey(gameOwnership => gameOwnership.Id);
        builder
            .Property(gameOwnership => gameOwnership.Id)
            .HasConversion(id => id.Value, value => GameOwnershipId.From(value));

        builder
            .Property(gameOwnership => gameOwnership.OwnerId)
            .HasConversion(id => id.Value, value => OwnerId.From(value));
        builder
            .Property(gameOwnership => gameOwnership.GameId)
            .HasConversion(id => id.Value, value => GameId.From(value));
        builder
            .Property(gameOwnership => gameOwnership.OrderId)
            .HasConversion(id => id.Value, value => OrderId.From(value));
        builder.Property(gameOwnership => gameOwnership.AcquiredAt).HasPrecision(0);

        builder.HasIndex(gameOwnership => new { gameOwnership.OwnerId, gameOwnership.GameId }).IsUnique();
    }
}
