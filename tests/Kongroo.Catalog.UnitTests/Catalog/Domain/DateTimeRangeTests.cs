using Kongroo.Catalog.Domain;
using Shouldly;

namespace Kongroo.Catalog.UnitTests.Catalog.Domain;

public sealed class DateTimeRangeTests
{
    [Fact]
    public void From_WithStartBeforeEnd_ShouldCreateRange()
    {
        // Arrange
        var start = new DateTimeOffset(2026, 3, 30, 12, 0, 0, TimeSpan.Zero);
        var end = start.AddHours(4);

        // Act
        var range = DateTimeRange.From(start, end);

        // Assert
        range.Start.ShouldBe(start);
        range.End.ShouldBe(end);
    }

    [Fact]
    public void From_WhenStartIsEqualToEnd_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var start = new DateTimeOffset(2026, 3, 30, 12, 0, 0, TimeSpan.Zero);

        // Act
        var exception = Should.Throw<ArgumentOutOfRangeException>(() => DateTimeRange.From(start, start));

        // Assert
        exception.ParamName.ShouldBe("end");
    }

    [Fact]
    public void Contains_WhenInstantIsInsideHalfOpenRange_ShouldReturnTrue()
    {
        // Arrange
        var start = new DateTimeOffset(2026, 3, 30, 12, 0, 0, TimeSpan.Zero);
        var range = DateTimeRange.From(start, start.AddHours(2));

        // Act
        var contains = range.Contains(start.AddHours(1));

        // Assert
        contains.ShouldBeTrue();
    }

    [Fact]
    public void Contains_WhenInstantMatchesRangeEnd_ShouldReturnFalse()
    {
        // Arrange
        var start = new DateTimeOffset(2026, 3, 30, 12, 0, 0, TimeSpan.Zero);
        var end = start.AddHours(2);
        var range = DateTimeRange.From(start, end);

        // Act
        var contains = range.Contains(end);

        // Assert
        contains.ShouldBeFalse();
    }

    [Fact]
    public void Overlaps_WithSeparatedRanges_ShouldReturnFalse()
    {
        // Arrange
        var start = new DateTimeOffset(2026, 3, 30, 12, 0, 0, TimeSpan.Zero);
        var left = DateTimeRange.From(start, start.AddHours(2));
        var right = DateTimeRange.From(start.AddHours(3), start.AddHours(5));

        // Act
        var overlaps = left.Overlaps(right);

        // Assert
        overlaps.ShouldBeFalse();
    }

    [Fact]
    public void Overlaps_WithAdjacentRanges_ShouldReturnFalse()
    {
        // Arrange
        var start = new DateTimeOffset(2026, 3, 30, 12, 0, 0, TimeSpan.Zero);
        var left = DateTimeRange.From(start, start.AddHours(2));
        var right = DateTimeRange.From(start.AddHours(2), start.AddHours(4));

        // Act
        var overlaps = left.Overlaps(right);

        // Assert
        overlaps.ShouldBeFalse();
    }
}

