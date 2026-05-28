namespace Kongroo.Catalog.Domain;

public sealed record GameTitle(string Value)
{
    public const int MinLength = 2;
    public const int MaxLength = 256;

    public static GameTitle From(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return value.Length switch
        {
            < MinLength => throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                $"Game title must be at least {MinLength} characters long."
            ),
            > MaxLength => throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                $"Game title must be at most {MaxLength} characters long."
            ),
            _ => new GameTitle(value),
        };
    }
}

