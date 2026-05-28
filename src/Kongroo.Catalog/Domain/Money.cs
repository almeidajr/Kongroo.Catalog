namespace Kongroo.Catalog.Domain;

public sealed record Money(decimal Amount, Currency Currency)
{
    public static readonly Money Zero = new(0m, Currency.Usd);

    public static Money From(decimal amount, Currency currency)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount must be non-negative.");
        }

        return new Money(amount, currency);
    }

    public Money ApplyDiscount(Percentage percentage)
    {
        ArgumentNullException.ThrowIfNull(percentage);

        var discountedAmount = decimal.Round(Amount - percentage.ApplyTo(Amount), 2, MidpointRounding.AwayFromZero);

        return From(discountedAmount, Currency);
    }
}

