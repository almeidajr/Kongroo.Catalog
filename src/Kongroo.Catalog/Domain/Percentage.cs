using System.Numerics;

namespace Kongroo.Catalog.Domain;

public sealed record Percentage(decimal Value) : IMinMaxValue<Percentage>
{
    private const decimal MinAllowedValue = 0m;
    private const decimal MaxAllowedValue = 100m;

    public static Percentage MinValue { get; } = new(MinAllowedValue);
    public static Percentage MaxValue { get; } = new(MaxAllowedValue);

    public static Percentage From(decimal value)
    {
        if (value is < MinAllowedValue or > MaxAllowedValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                $"Percentage must be between {MinAllowedValue} and {MaxAllowedValue}."
            );
        }

        return new Percentage(value);
    }

    public decimal ToFraction() => Value / 100m;

    public decimal ApplyTo(decimal amount) => amount * ToFraction();
}

