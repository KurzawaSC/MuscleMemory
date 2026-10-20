using MuscleMemory.Charting;

namespace MuscleMemory.Tests.Charting;

public sealed class DateScaleTests
{
    [Fact]
    public void For_SingleDay_IsThatDay()
    {
        var day = new DateOnly(2026, 10, 7);

        var ticks = DateScale.For(day, day);

        Assert.Equal([day], ticks.Dates);
        Assert.Equal(DateTickUnit.Day, ticks.Unit);
    }

    [Fact]
    public void For_ThreeWeeks_TicksOnMondays()
    {
        var ticks = DateScale.For(new DateOnly(2026, 9, 16), new DateOnly(2026, 10, 7));

        Assert.Equal(DateTickUnit.Day, ticks.Unit);
        Assert.Equal([new DateOnly(2026, 9, 21), new DateOnly(2026, 9, 28), new DateOnly(2026, 10, 5)], ticks.Dates);
        Assert.All(ticks.Dates, date => Assert.Equal(DayOfWeek.Monday, date.DayOfWeek));
    }

    [Fact]
    public void For_ThreeMonths_TicksOnMonthStarts()
    {
        var ticks = DateScale.For(new DateOnly(2026, 7, 19), new DateOnly(2026, 10, 7));

        Assert.Equal(DateTickUnit.Month, ticks.Unit);
        Assert.Equal([new DateOnly(2026, 8, 1), new DateOnly(2026, 9, 1), new DateOnly(2026, 10, 1)], ticks.Dates);
    }

    [Fact]
    public void For_SixMonths_TicksEveryOtherMonth()
    {
        var ticks = DateScale.For(new DateOnly(2026, 4, 19), new DateOnly(2026, 10, 7));

        Assert.Equal(DateTickUnit.Month, ticks.Unit);
        Assert.Equal([new DateOnly(2026, 5, 1), new DateOnly(2026, 7, 1), new DateOnly(2026, 9, 1)], ticks.Dates);
    }

    [Fact]
    public void For_OneYear_TicksEveryQuarter()
    {
        var ticks = DateScale.For(new DateOnly(2025, 10, 19), new DateOnly(2026, 10, 7));

        Assert.Equal(DateTickUnit.Month, ticks.Unit);
        Assert.Equal([new DateOnly(2026, 1, 1), new DateOnly(2026, 4, 1), new DateOnly(2026, 7, 1), new DateOnly(2026, 10, 1)], ticks.Dates);
    }

    [Fact]
    public void For_ThreeYears_TicksOnNewYear()
    {
        var ticks = DateScale.For(new DateOnly(2023, 10, 7), new DateOnly(2026, 10, 7));

        Assert.Equal(DateTickUnit.Year, ticks.Unit);
        Assert.Equal([new DateOnly(2024, 1, 1), new DateOnly(2025, 1, 1), new DateOnly(2026, 1, 1)], ticks.Dates);
    }

    [Fact]
    public void For_Decades_WidensTheYearStep()
    {
        var ticks = DateScale.For(new DateOnly(1990, 1, 1), new DateOnly(2026, 1, 1));

        Assert.Equal(DateTickUnit.Year, ticks.Unit);
        Assert.Equal([1990, 2000, 2010, 2020], ticks.Dates.Select(date => date.Year));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(13)]
    [InlineData(45)]
    [InlineData(60)]
    [InlineData(200)]
    [InlineData(400)]
    [InlineData(800)]
    [InlineData(1500)]
    public void For_AnySpan_HasBetweenTwoAndFourTicksInsideIt(int spanDays)
    {
        var first = new DateOnly(2024, 2, 10);
        var last = first.AddDays(spanDays);

        var ticks = DateScale.For(first, last);

        Assert.InRange(ticks.Dates.Count, 2, DateScale.MaxTicks);
        Assert.All(ticks.Dates, date => Assert.InRange(date, first, last));
    }

    [Fact]
    public void For_FirstAfterLast_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DateScale.For(new DateOnly(2026, 1, 2), new DateOnly(2026, 1, 1)));
    }
}
