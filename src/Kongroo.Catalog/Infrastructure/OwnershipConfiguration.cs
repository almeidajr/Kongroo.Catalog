using Kongroo.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kongroo.Catalog.Infrastructure;

public sealed class OwnershipConfiguration : IEntityTypeConfiguration<Ownership>
{
    public void Configure(EntityTypeBuilder<Ownership> builder)
    {
        builder.HasKey(ownership => ownership.Id);
        builder.Property(ownership => ownership.Id).HasConversion(id => id.Value, value => OwnershipId.From(value));

        builder
            .Property(ownership => ownership.CustomerId)
            .HasConversion(id => id.Value, value => CustomerId.From(value));
        builder.Property(ownership => ownership.GameId).HasConversion(id => id.Value, value => GameId.From(value));
        builder.Property(ownership => ownership.OrderId).HasConversion(id => id.Value, value => OrderId.From(value));
        builder.Property(ownership => ownership.AcquiredAt).HasPrecision(0);

        builder.HasIndex(ownership => new { ownership.CustomerId, ownership.GameId }).IsUnique();
    }
}
