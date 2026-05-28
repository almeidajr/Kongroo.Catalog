using Kongroo.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kongroo.Catalog.Infrastructure;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(order => order.Id);
        builder.Property(order => order.Id).HasConversion(id => id.Value, value => OrderId.From(value));

        builder.Property(order => order.BuyerId).HasConversion(id => id.Value, value => BuyerId.From(value));
        builder.Property(order => order.PurchasedAt).HasPrecision(0);

        builder.ComplexProperty(
            order => order.Total,
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
            order => order.Lines,
            lineBuilder =>
            {
                lineBuilder.ToTable("order_lines");

                lineBuilder.HasKey(nameof(OrderId), nameof(GameId));
                lineBuilder
                    .Property<OrderId>(nameof(OrderId))
                    .HasConversion(id => id.Value, value => OrderId.From(value));
                lineBuilder.Property(line => line.GameId).HasConversion(id => id.Value, value => GameId.From(value));

                lineBuilder
                    .Property(line => line.GameTitle)
                    .HasConversion(title => title.Value, value => GameTitle.From(value))
                    .HasMaxLength(GameTitle.MaxLength);

                lineBuilder.OwnsOne(
                    line => line.ListPrice,
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

                lineBuilder.OwnsOne(
                    line => line.FinalPrice,
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

                lineBuilder
                    .Property<PromotionId?>(line => line.AppliedPromotionId)
                    .HasConversion(
                        promotionId => promotionId == null ? (Guid?)null : promotionId.Value,
                        value => value == null ? null : PromotionId.From(value.Value)
                    );
            }
        );

        builder.Navigation(order => order.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

