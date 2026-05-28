namespace Kongroo.Catalog.Domain;

public sealed record GameDescription(string Value)
{
    public const int MinLength = 2;
    public const int MaxLength = 2048;

    public static GameDescription From(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return value.Length switch
        {
            < MinLength => throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                $"Game description must be at least {MinLength} characters long."
            ),
            > MaxLength => throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                $"Game description must be at most {MaxLength} characters long."
            ),
            _ => new GameDescription(value),
        };
    }
}

