using Kongroo.BuildingBlocks.Domain;

namespace Kongroo.Catalog.Domain;

public sealed class Promotion : Entity<PromotionId>
{
    private Promotion() { }

    public required Percentage Discount { get; init; }

    public required DateTimeRange ActiveRange { get; init; }

    public static Promotion Create(Percentage discount, DateTimeRange activeRange)
    {
        ArgumentNullException.ThrowIfNull(discount);
        ArgumentNullException.ThrowIfNull(activeRange);

        return new Promotion
        {
            Id = PromotionId.Create(),
            Discount = discount,
            ActiveRange = activeRange,
        };
    }
}

