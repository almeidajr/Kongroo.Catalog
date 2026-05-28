using Kongroo.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kongroo.Catalog.Infrastructure;

public sealed class GameConfiguration : IEntityTypeConfiguration<Game>
{
    public void Configure(EntityTypeBuilder<Game> builder)
    {
        builder.HasKey(game => game.Id);
        builder.Property(game => game.Id).HasConversion(id => id.Value, value => GameId.From(value));

        builder
            .Property(game => game.Title)
            .HasConversion(title => title.Value, value => GameTitle.From(value))
            .HasMaxLength(GameTitle.MaxLength);
        builder
            .Property(game => game.Description)
            .HasConversion(description => description.Value, value => GameDescription.From(value))
            .HasMaxLength(GameDescription.MaxLength);
        builder.Property(game => game.Status).HasConversion<string>().HasMaxLength(16);

        builder.ComplexProperty(
            game => game.Price,
            moneyBuilder =>
            {
                moneyBuilder.Property(money => money.Amount).HasPrecision(18, 2);

                moneyBuilder
                    .Property(money => money.Currency)
                    .HasConversion(
                        currency => CurrencyMappings.ToCode(currency),
                        code => CurrencyMappings.FromCode(code)
                    )
                    .HasMaxLength(CurrencyMappings.Length)
                    .IsFixedLength();
            }
        );

        builder.OwnsMany(
            game => game.Promotions,
            promotionBuilder =>
            {
                promotionBuilder.ToTable("promotions");

                promotionBuilder.HasKey(promotion => promotion.Id);
                promotionBuilder
                    .Property(promotion => promotion.Id)
                    .HasConversion(id => id.Value, value => PromotionId.From(value));

                promotionBuilder
                    .Property(promotion => promotion.Discount)
                    .HasConversion(discount => discount.Value, value => Percentage.From(value))
                    .HasPrecision(5, 2);

                promotionBuilder.OwnsOne(
                    promotion => promotion.ActiveRange,
                    rangeBuilder =>
                    {
                        rangeBuilder.Property(range => range.Start).HasPrecision(0);
                        rangeBuilder.Property(range => range.End).HasPrecision(0);
                    }
                );
            }
        );

        builder.Navigation(game => game.Promotions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

