namespace Kongroo.Catalog.Domain;

public sealed record DateTimeRange(DateTimeOffset Start, DateTimeOffset End)
{
    public static DateTimeRange From(DateTimeOffset start, DateTimeOffset end)
    {
        if (start >= end)
        {
            throw new ArgumentOutOfRangeException(nameof(end), end, "End must be greater than start.");
        }

        return new DateTimeRange(start, end);
    }

    public bool Contains(DateTimeOffset instant) => instant >= Start && instant < End;

    public bool Overlaps(DateTimeRange other)
    {
        ArgumentNullException.ThrowIfNull(other);

        return Start < other.End && other.Start < End;
    }
}
